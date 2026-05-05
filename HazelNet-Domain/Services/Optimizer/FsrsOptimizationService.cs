using HazelNet.Service.Optimizer;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet.Services.Optimizer;
// highkey just asked claude to make the documentation becuase mine would be very messy
// =====================================================================================
// FsrsOptimizationService — user-level FSRS weight optimizer
// =====================================================================================
//
// PURPOSE
// -------
// Trains a set of 21 FSRS weights that are personalized to a specific user's review
// history. The trained weights replace the generic defaults in FSRSParameters.W and
// are then used by the scheduler to produce more accurate due-date predictions.
//
// INPUTS
// ------
// The public API takes IEnumerable<int> cardIds — the IDs of every card whose review
// history should be included in training.
//
// How to get cardIds:
//   • Single deck:  var cardIds = await cardRepo.GetAllCardByDeckIdAsync(deckId);
//                                               .Select(c => c.Id);
//   • All user decks (no IUserRepository yet — query directly until one is added):
//       var cardIds = await dbContext.Cards
//                         .Where(c => c.Deck.UserId == userId)
//                         .Select(c => c.Id)
//                         .ToListAsync();
//
// The service resolves the rest internally:
//   cardIds → ReviewHistory  (one per card, via IReviewHistoryRepository.GetReviewHistoryByCardIdAsync)
//           → ReviewLogs     (all histories in one batch, via the injected log-fetcher delegate)
//
// Cards with no ReviewHistory row are silently skipped. Histories with fewer than 2
// ReviewLogs are skipped by the data processor (not enough data to form a sample).
//
// OUTPUTS
// -------
// OptimizeWeightsAsync        → double[] of length 21  (weights only)
// OptimizeWeightsWithDiagnosticsAsync → OptimizationResult:
//     .Weights             double[21]  trained weight values
//     .HistoriesRequested  int         how many cardIds were submitted
//     .HistoriesLoaded     int         how many had a ReviewHistory row in the DB
//     .LogsLoaded          int         total ReviewLogs that made it into training
//     .SamplesGenerated    int         windowed samples produced by the data processor
//
// The 21 trained weights are a direct drop-in replacement for Weights.Default() and
// share the same array layout.
//
// SAVING THE OUTPUT
// -----------------
// Trained weights belong on User.FSRSParameters.W. Once IUserRepository exists the
// write path is:
//
//     var weights = await optimizer.OptimizeWeightsAsync(cardIds);
//     user.FSRSParameters.W = weights;
//     await userRepository.UpdateAsync(user);
//
// Until IUserRepository is wired up, write directly through the DbContext:
//
//     var weights = await optimizer.OptimizeWeightsAsync(cardIds);
//     var user = await dbContext.User.FindAsync(userId);
//     user.FSRSParameters.W = weights;
//     await dbContext.SaveChangesAsync();
//
// WIRING (production DI)
// ----------------------
//
//   var optimizer = new FsrsOptimizationService(
//       reviewHistoryRepo,
//       async historyIds =>
//       {
//           var idList = historyIds.ToList();
//           var logs = await dbContext.ReviewLogs
//               .Where(r => idList.Contains(r.ReviewHistoryId))
//               .ToListAsync();
//           return logs.GroupBy(r => r.ReviewHistoryId)
//                      .ToDictionary(g => g.Key, g => g.ToList());
//       });
//
//   var result = await optimizer.OptimizeWeightsWithDiagnosticsAsync(cardIds);
//
// WIRING (direct / unit tests — no DB)
// -------------------------------------
//   var optimizer = new FsrsOptimizationService();
//   var weights   = optimizer.OptimizeWeights(reviewHistories);
//
// learningRate and regularization have sensible defaults (0.05 each); only adjust
// them if you have a concrete reason to.
// =====================================================================================

// Diagnostic summary returned alongside the trained weights so callers can see
// exactly what data made it into training without attaching a debugger.
public class OptimizationResult
{
    public double[] Weights { get; init; } = Array.Empty<double>();
    public int HistoriesRequested { get; init; }
    public int HistoriesLoaded { get; init; }
    public int LogsLoaded { get; init; }
    public int SamplesGenerated { get; init; }
}

public class FsrsOptimizationService
{
    private readonly FsrsDataProcessor _processor;
    private readonly FsrsTrainer _trainer;
    private readonly IReviewHistoryRepository? _reviewHistoryRepository;

    // Batched log fetcher: takes a set of review history ids, returns a dictionary
    // keyed by ReviewHistoryId. lets callers issue a single "WHERE Id IN (...)" query
    // and avoid the N+1 pattern that the per-id approach forced.
    private readonly Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>>? _reviewLogFetcher;

    // Constructor for DB-backed usage. accepts the history repo plus a delegate that
    // knows how to load logs in bulk. the delegate keeps the optimizer ignorant of
    // EF Core / IReviewLogRepository while still allowing efficient batched queries.
    public FsrsOptimizationService(
        IReviewHistoryRepository reviewHistoryRepository,
        Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>> reviewLogFetcher,
        double learningRate = 0.05,
        double regularization = 0.05)
    {
        _reviewHistoryRepository = reviewHistoryRepository ?? throw new ArgumentNullException(nameof(reviewHistoryRepository));
        _reviewLogFetcher = reviewLogFetcher ?? throw new ArgumentNullException(nameof(reviewLogFetcher));
        _processor = new FsrsDataProcessor();
        _trainer = new FsrsTrainer(learningRate, regularization);
    }

    // Convenience overload: accepts a per-id fetcher and adapts it to the batched signature.
    // Useful when you don't have an easy way to issue a batched query (e.g. when using an
    // existing IReviewLogRepository.GetReviewLogsByReviewHistoryId without modifying it).
    // Internally still does N round-trips, so prefer the batched constructor when possible.
    public FsrsOptimizationService(
        IReviewHistoryRepository reviewHistoryRepository,
        Func<int, Task<List<ReviewLog>>> reviewLogFetcherPerId,
        double learningRate = 0.05,
        double regularization = 0.05)
        : this(
            reviewHistoryRepository,
            AdaptPerIdFetcher(reviewLogFetcherPerId ?? throw new ArgumentNullException(nameof(reviewLogFetcherPerId))),
            learningRate,
            regularization)
    {
    }

    // Parameterless constructor for direct usage (e.g. unit tests that supply data manually).
    public FsrsOptimizationService(double learningRate = 0.05, double regularization = 0.05)
    {
        _processor = new FsrsDataProcessor();
        _trainer = new FsrsTrainer(learningRate, regularization);
    }

    // Fetches review histories from DB using the injected repo + delegate, then optimizes.
    // Returns just the weights for backwards compatibility; use OptimizeWeightsWithDiagnosticsAsync
    // if you want a summary of what actually got loaded.
    //
    // Trained weights should be saved to User.FSRSParameters.W — see file header for the
    // full save pattern.
    public async Task<double[]> OptimizeWeightsAsync(IEnumerable<int> cardIds,
        int epochs = 5, int batchSize = 4096)
    {
        var result = await OptimizeWeightsWithDiagnosticsAsync(cardIds, epochs, batchSize);
        return result.Weights;
    }

    // Diagnostic version: returns weights plus a summary of histories/logs/samples loaded.
    // Use this for integration tests and live smoke tests so failures point at the layer
    // that actually broke (no histories returned? no logs joined? no samples generated?).
    //
    // Trained weights should be saved to User.FSRSParameters.W — see file header for the
    // full save pattern.
    public async Task<OptimizationResult> OptimizeWeightsWithDiagnosticsAsync(IEnumerable<int> cardIds,
        int epochs = 5, int batchSize = 4096)
    {
        if (_reviewHistoryRepository == null || _reviewLogFetcher == null)
            throw new InvalidOperationException(
                "Cannot fetch review history from DB: repository or log fetcher was not injected. " +
                "Use the constructor that accepts IReviewHistoryRepository and a log-fetcher delegate.");

        if (cardIds == null)
            throw new ArgumentNullException(nameof(cardIds));

        // Materialize once so we can count requested ids without re-enumerating the source.
        var cardIdList = cardIds as IList<int> ?? cardIds.ToList();

        // Step 1: load histories one per card. the history repo only exposes a per-card lookup,
        // so this loop is unavoidable without a new repo method. this is the cheap leg of the
        // query though - typically a single indexed lookup per card.
        var reviewHistories = new List<ReviewHistory>();
        foreach (var cardId in cardIdList)
        {
            var history = await _reviewHistoryRepository.GetReviewHistoryByCardIdAsync(cardId);
            if (history == null) continue;

            reviewHistories.Add(history);
        }

        if (reviewHistories.Count == 0)
            throw new ArgumentException("No valid review histories found for the provided card IDs.", nameof(cardIds));

        // Step 2: bulk-fetch all logs for the loaded histories in one round-trip via the delegate.
        // This is the meaningful win over the previous per-id loop - avoids N round-trips when
        // the caller wired the delegate up to a `WHERE ReviewHistoryId IN (...)` style query.
        var historyIds = reviewHistories.Select(h => h.Id).ToList();
        var logsByHistoryId = await _reviewLogFetcher(historyIds);

        // Step 3: stitch logs back onto each history. histories with no logs get an empty list,
        // and the data processor will skip them (it requires >=2 reviews per history to emit a sample).
        int totalLogsLoaded = 0;
        foreach (var history in reviewHistories)
        {
            if (logsByHistoryId != null && logsByHistoryId.TryGetValue(history.Id, out var logs))
            {
                history.ReviewLogs = logs;
                totalLogsLoaded += logs.Count;
            }
            else
            {
                history.ReviewLogs = new List<ReviewLog>();
            }
        }

        Console.WriteLine($"[FsrsOptimizationService] Loaded {reviewHistories.Count}/{cardIdList.Count} histories, {totalLogsLoaded} review logs total.");

        var samples = _processor.ProcessHistoryToSamples(reviewHistories);

        if (samples.Count == 0)
            throw new InvalidOperationException(
                $"Loaded {reviewHistories.Count} histories with {totalLogsLoaded} logs but no training samples could be generated. " +
                "Each history needs at least 2 review logs to produce a sample.");

        var weights = _trainer.Train(samples, epochs, batchSize);

        return new OptimizationResult
        {
            Weights = weights,
            HistoriesRequested = cardIdList.Count,
            HistoriesLoaded = reviewHistories.Count,
            LogsLoaded = totalLogsLoaded,
            SamplesGenerated = samples.Count
        };
    }

    // Direct method that accepts pre-loaded review histories (unchanged core logic).
    public double[] OptimizeWeights(IEnumerable<ReviewHistory> reviewHistories,
        int epochs = 5, int batchSize = 4096)
    {
        if (reviewHistories == null)
            throw new ArgumentNullException(nameof(reviewHistories));

        var samples = _processor.ProcessHistoryToSamples(reviewHistories);
        return _trainer.Train(samples, epochs, batchSize);
    }

    public double[] OptimizeWeights(IEnumerable<ReviewHistory> reviewHistories)
    {
        return OptimizeWeights(reviewHistories, epochs: 5, batchSize: 4096);
    }

    // Adapter so the per-id convenience constructor can reuse the batched code path.
    // Sequentially awaits each id - acceptable for small id sets, but if you care about
    // throughput, supply a real batched delegate to the primary constructor instead.
    private static Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>>
        AdaptPerIdFetcher(Func<int, Task<List<ReviewLog>>> perIdFetcher)
    {
        return async ids =>
        {
            var dict = new Dictionary<int, List<ReviewLog>>();
            foreach (var id in ids)
            {
                var logs = await perIdFetcher(id);
                dict[id] = logs ?? new List<ReviewLog>();
            }
            return dict;
        };
    }
}
