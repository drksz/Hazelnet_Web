using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HazelNet.Services.Optimizer;
using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;
using HazelNet_Infrastracture.DBServices.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HazelNet.Tests.Optimizer
{
    /// <summary>
    /// Layer-3 smoke tests: full FSRS optimizer pipeline against real Postgres data
    /// seeded by HazelNet-Seeder. All tests are Skip="manual-only" — run them by
    /// hand after dotnet run --project HazelNet-Seeder has populated the DB.
    ///
    /// Expected seed state (Username == "seed-user"):
    ///   1 user · 3 decks · 15 cards · 15 histories · 90 review logs (6 per history)
    ///   Each history produces 5 windowed training samples → 75 total.
    /// </summary>
    public class FsrsOptimizerPostgresTests
    {
        private const string ConnStr =
            "Host=localhost;Port=5432;Database=HazelNetDb;Username=postgres;Password=Password";

        private static ApplicationDbContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnStr)
                .Options;
            return new ApplicationDbContext(options);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Finds seed-user and returns all card IDs across their decks.
        /// Throws with a clear message if the seeder has not been run.
        /// </summary>
        private static async Task<List<int>> GetSeedUserCardIdsAsync(ApplicationDbContext ctx)
        {
            var user = await ctx.User
                .Include(u => u.Decks!)
                    .ThenInclude(d => d.Cards)
                .FirstOrDefaultAsync(u => u.Username == "seed-user");

            if (user is null)
                throw new InvalidOperationException(
                    "seed-user not found — run HazelNet-Seeder first.");

            return user.Decks!
                .SelectMany(d => d.Cards)
                .Select(c => c.Id)
                .ToList();
        }

        // ------------------------------------------------------------------ tests

        /// <summary>
        /// Full pipeline smoke test. Verifies that the optimizer produces well-formed
        /// weights and that every diagnostic counter exactly matches the seeded data.
        /// </summary>
        [Fact(Skip = "manual-only")]
        public async Task SeedUser_FullPipeline_ProducesValidWeightsAndAccurateCounters()
        {
            // Arrange
            await using var ctx = BuildContext();

            var cardIds = await GetSeedUserCardIdsAsync(ctx);
            cardIds.Should().HaveCount(15, "seeder creates 3 decks × 5 cards each");

            var historyRepo = new ReviewHistoryRepository(ctx);

            Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>> logFetcher =
                async historyIds =>
                {
                    var idList = historyIds.ToList();
                    var logs = await ctx.ReviewLogs
                        .Where(r => idList.Contains(r.ReviewHistoryId))
                        .ToListAsync();

                    IReadOnlyDictionary<int, List<ReviewLog>> dict = logs
                        .GroupBy(r => r.ReviewHistoryId)
                        .ToDictionary(g => g.Key, g => g.ToList());
                    return dict;
                };

            var optimizer = new FsrsOptimizationService(historyRepo, logFetcher);

            // Act
            var result = await optimizer.OptimizeWeightsWithDiagnosticsAsync(
                cardIds, epochs: 1, batchSize: 32);

            // Assert — data counters match the seeder's structure exactly
            result.HistoriesRequested.Should().Be(15,
                "one history per card, 15 cards total");
            result.HistoriesLoaded.Should().Be(15,
                "every seeded card has a ReviewHistory row");
            result.LogsLoaded.Should().Be(90,
                "6 ReviewLogs per history × 15 histories");
            result.SamplesGenerated.Should().Be(75,
                "6 logs per history yields 5 windowed samples, 15 histories × 5 = 75");

            // Assert — weights are numerically sane
            result.Weights.Should().HaveCount(21);
            result.Weights.Should().NotContain(double.NaN);
            result.Weights.Should().NotContain(double.NegativeInfinity);
            result.Weights.Should().NotContain(double.PositiveInfinity);
            result.Weights[0].Should().BeGreaterThan(0,
                "first weight initialises from a positive prior and should not collapse to zero");
        }

        /// <summary>
        /// Verifies the batched delegate is invoked exactly once for all 15 histories,
        /// proving the optimizer does not fall back to a per-id N+1 pattern.
        /// </summary>
        [Fact(Skip = "manual-only")]
        public async Task SeedUser_BatchedDelegate_IsInvokedExactlyOnce()
        {
            // Arrange
            await using var ctx = BuildContext();

            var cardIds = await GetSeedUserCardIdsAsync(ctx);

            var historyRepo = new ReviewHistoryRepository(ctx);

            int fetcherCallCount = 0;
            Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>> logFetcher =
                async historyIds =>
                {
                    fetcherCallCount++;
                    var idList = historyIds.ToList();

                    var logs = await ctx.ReviewLogs
                        .Where(r => idList.Contains(r.ReviewHistoryId))
                        .ToListAsync();

                    IReadOnlyDictionary<int, List<ReviewLog>> dict = logs
                        .GroupBy(r => r.ReviewHistoryId)
                        .ToDictionary(g => g.Key, g => g.ToList());
                    return dict;
                };

            var optimizer = new FsrsOptimizationService(historyRepo, logFetcher);

            // Act
            await optimizer.OptimizeWeightsAsync(cardIds, epochs: 1, batchSize: 32);

            // Assert
            fetcherCallCount.Should().Be(1,
                "all history IDs must be batched into a single WHERE...IN round-trip");
        }

        /// <summary>
        /// Partial-match guard: mixing real seed-user card IDs with non-existent IDs
        /// should load only the real histories, skip the missing ones silently, and
        /// still produce valid weights from the real data.
        /// </summary>
        [Fact(Skip = "manual-only")]
        public async Task SeedUser_PartialCardIds_LoadsOnlyExistingHistories()
        {
            // Arrange
            await using var ctx = BuildContext();

            var realCardIds = await GetSeedUserCardIdsAsync(ctx);

            var historyRepo = new ReviewHistoryRepository(ctx);

            Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>> logFetcher =
                async historyIds =>
                {
                    var idList = historyIds.ToList();
                    var logs = await ctx.ReviewLogs
                        .Where(r => idList.Contains(r.ReviewHistoryId))
                        .ToListAsync();

                    IReadOnlyDictionary<int, List<ReviewLog>> dict = logs
                        .GroupBy(r => r.ReviewHistoryId)
                        .ToDictionary(g => g.Key, g => g.ToList());
                    return dict;
                };

            var optimizer = new FsrsOptimizationService(historyRepo, logFetcher);

            // Mix in two card IDs that will never exist.
            var mixedIds = realCardIds.Concat(new[] { int.MaxValue - 1, int.MaxValue }).ToList();

            // Act
            var result = await optimizer.OptimizeWeightsWithDiagnosticsAsync(
                mixedIds, epochs: 1, batchSize: 32);

            // Assert — requested includes the phantom IDs, loaded excludes them
            result.HistoriesRequested.Should().Be(17,
                "15 real card IDs + 2 non-existent ones");
            result.HistoriesLoaded.Should().Be(15,
                "only the 15 seeded histories exist in the DB");
            result.LogsLoaded.Should().Be(90,
                "all 90 seeded logs are still loaded for the real histories");

            result.Weights.Should().HaveCount(21);
            result.Weights.Should().NotContain(double.NaN);
        }
    }
}
