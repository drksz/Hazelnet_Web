using HazelNet.Service.Optimizer;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet.Services.Optimizer;

//an "API" service for optimizing FSRS weights.

//usage (with DI): 
//var optimizer = new FsrsOptimizationService(reviewHistoryRepo, reviewLogRepo);
//var trainedWeights = await optimizer.OptimizeWeightsAsync(cardIds);

//usage (direct, for testing):
//var optimizer = new FsrsOptimizationService();
//var trainedWeights = optimizer.OptimizeWeights(reviewHistories);

//we can also modify the hyperparams, but defaults are reasonable so only use that.
public class FsrsOptimizationService
{
    private readonly FsrsDataProcessor _processor;
    private readonly FsrsTrainer _trainer;
    private readonly IReviewHistoryRepository? _reviewHistoryRepository;
    private readonly IReviewLogRepository? _reviewLogRepository;

    //constructor with repository injection for DB-backed review history retrieval
    public FsrsOptimizationService(
        IReviewHistoryRepository reviewHistoryRepository,
        IReviewLogRepository reviewLogRepository,
        double learningRate = 0.05, 
        double regularization = 0.05)
    {
        _reviewHistoryRepository = reviewHistoryRepository ?? throw new ArgumentNullException(nameof(reviewHistoryRepository));
        _reviewLogRepository = reviewLogRepository ?? throw new ArgumentNullException(nameof(reviewLogRepository));
        _processor = new FsrsDataProcessor();
        _trainer = new FsrsTrainer(learningRate, regularization);
    }

    //parameterless constructor for direct usage (e.g. unit tests that supply data manually)
    public FsrsOptimizationService(double learningRate = 0.05, double regularization = 0.05)
    {
        _processor = new FsrsDataProcessor();
        _trainer = new FsrsTrainer(learningRate, regularization);
    }

    //fetches review histories from DB using the new repository commands, then optimizes
    public async Task<double[]> OptimizeWeightsAsync(IEnumerable<int> cardIds,
        int epochs = 5, int batchSize = 4096)
    {
        if (_reviewHistoryRepository == null || _reviewLogRepository == null)
            throw new InvalidOperationException(
                "Cannot fetch review history from DB: repositories were not injected. " +
                "Use the constructor that accepts IReviewHistoryRepository and IReviewLogRepository.");

        if (cardIds == null)
            throw new ArgumentNullException(nameof(cardIds));

        var reviewHistories = new List<ReviewHistory>();

        foreach (var cardId in cardIds)
        {
            var history = await _reviewHistoryRepository.GetReviewHistoryByCardIdAsync(cardId);
            if (history == null) continue;

            //populate the review logs from the DB using the new repository command
            var logs = await _reviewLogRepository.GetReviewLogsByReviewHistoryIdAsync(history.Id);
            history.ReviewLogs = logs;

            reviewHistories.Add(history);
        }

        if (reviewHistories.Count == 0)
            throw new ArgumentException("No valid review histories found for the provided card IDs.", nameof(cardIds));

        return OptimizeWeights(reviewHistories, epochs, batchSize);
    }

    //direct method that accepts pre-loaded review histories (unchanged core logic)
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
}
