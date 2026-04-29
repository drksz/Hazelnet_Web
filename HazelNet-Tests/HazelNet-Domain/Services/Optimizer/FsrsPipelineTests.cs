using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HazelNet.Service.Optimizer;
using HazelNet.Services.Optimizer;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet.Tests.Optimizer
{
    public class FsrsPipelineTests
    {
        // ---------------------------------------------------------
        // 1. Initialization & Sanity
        // ---------------------------------------------------------

        [Fact]
        public void Pipeline_Components_InitializeCorrectly()
        {
            var processor = new FsrsDataProcessor();
            var trainer = new FsrsTrainer();
            var optimizer = new FsrsOptimizationService();

            processor.Should().NotBeNull();
            trainer.Should().NotBeNull();
            optimizer.Should().NotBeNull();
        }

        [Fact]
        public void Pipeline_Components_InitializeCorrectly_WithRepoAndDelegate()
        {
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            var logFetcher = MakeBatchedFetcher(new Dictionary<int, List<ReviewLog>>());

            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, logFetcher);

            optimizer.Should().NotBeNull();
        }

        [Fact]
        public void Pipeline_Components_InitializeCorrectly_WithPerIdConvenienceDelegate()
        {
            // The convenience overload should accept a per-id fetcher.
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            Func<int, Task<List<ReviewLog>>> perId = _ => Task.FromResult(new List<ReviewLog>());

            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, perId);

            optimizer.Should().NotBeNull();
        }

        // ---------------------------------------------------------
        // 2. Integration Tests (Processor -> Trainer)
        // ---------------------------------------------------------

        [Fact]
        public void Pipeline_DataHandover_IsShapeCompliant()
        {
            // CRITICAL TEST: Verifies that the Processor output strictly matches
            // the Trainer's input requirements (Arrays of length 5, non-null).

            var processor = new FsrsDataProcessor();

            var historyItem = CreateReviewHistory(cardId: 1, reviewCount: 5);
            var histories = new List<ReviewHistory> { historyItem };

            var samples = processor.ProcessHistoryToSamples(histories);

            samples.Should().NotBeEmpty();
            foreach (var sample in samples)
            {
                sample.TimeHistory.Length.Should().Be(5);
                sample.RatingHistory.Length.Should().Be(5);
                sample.TargetInterval.Should().BeGreaterThan(0, "Pipeline passed an invalid interval to Trainer");
            }
        }

        [Fact]
        public void Integration_ProcessorToTrainer_FullFlow()
        {
            var processor = new FsrsDataProcessor();
            var trainer = new FsrsTrainer();

            var logs = new List<ReviewLog>
            {
                new ReviewLog { ElapsedDays = 0, Rating = Rating.Good, Review = DateTime.Now.AddDays(-20) },
                new ReviewLog { ElapsedDays = 2, Rating = Rating.Hard, Review = DateTime.Now.AddDays(-18) },
                new ReviewLog { ElapsedDays = 5, Rating = Rating.Good, Review = DateTime.Now.AddDays(-13) },
                new ReviewLog { ElapsedDays = 10, Rating = Rating.Easy, Review = DateTime.Now.AddDays(-3) },
                new ReviewLog { ElapsedDays = 3, Rating = Rating.Again, Review = DateTime.Now }
            };

            var historyItem = new ReviewHistory();
            foreach (var log in logs) historyItem.ReviewLogs.Add(log);

            var history = new List<ReviewHistory> { historyItem };

            var dataset = processor.ProcessHistoryToSamples(history);
            var weights = trainer.Train(dataset, epochs: 2, batchSize: 5);

            weights.Should().NotBeNull();
            weights.Length.Should().Be(21);
            weights.Should().NotContain(double.NaN);
            weights.Should().NotContain(double.NegativeInfinity).And.NotContain(double.PositiveInfinity);
            weights[0].Should().BeGreaterThan(0);
        }

        [Fact]
        public void Pipeline_FullFlow_ProducesValidWeights_OnGeneratedData()
        {
            var processor = new FsrsDataProcessor();
            var trainer = new FsrsTrainer();
            var histories = GenerateReviewHistories(count: 20);

            var samples = processor.ProcessHistoryToSamples(histories);
            var weights = trainer.Train(samples, epochs: 2, batchSize: 5);

            weights.Should().NotBeNull();
            weights.Length.Should().Be(21);
            weights.Should().NotContain(double.NaN);
            weights.Should().NotContain(double.NegativeInfinity).And.NotContain(double.PositiveInfinity);

            weights[0].Should().BeGreaterThan(0);
            weights[1].Should().BeGreaterThan(0);
            weights[2].Should().BeGreaterThan(0);
            weights[3].Should().BeGreaterThan(0);
        }

        // ---------------------------------------------------------
        // 3. Wrapper Service Tests (FsrsOptimizationService)
        // ---------------------------------------------------------

        [Fact]
        public void OptimizationService_EndToEnd_ReturnsOptimizedWeights()
        {
            var optimizer = new FsrsOptimizationService();
            var histories = GenerateReviewHistories(count: 50);

            var weights = optimizer.OptimizeWeights(histories, epochs: 1, batchSize: 10);

            weights.Should().NotBeNull();
            weights.Length.Should().Be(21);
        }

        [Fact]
        public void Pipeline_HandlesInsufficientData_Gracefully()
        {
            var processor = new FsrsDataProcessor();
            var trainer = new FsrsTrainer();

            var shortHistory = CreateReviewHistory(cardId: 1, reviewCount: 1);
            var shortHistories = new List<ReviewHistory> { shortHistory };

            var samples = processor.ProcessHistoryToSamples(shortHistories);

            samples.Should().BeEmpty();

            trainer.Invoking(t => t.Train(samples)).Should().Throw<ArgumentException>();
        }

        // ---------------------------------------------------------
        // 4. DB-Backed Async Pipeline Tests (delegate-based)
        // ---------------------------------------------------------

        [Fact]
        public async Task OptimizeWeightsAsync_WithValidCardIds_ReturnsOptimizedWeights()
        {
            // Arrange: stub history repo + an in-memory dictionary that the delegate reads.
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            var logsByHistoryId = new Dictionary<int, List<ReviewLog>>();

            for (int cardId = 1; cardId <= 10; cardId++)
            {
                var history = new ReviewHistory() { Id = cardId };
                reviewHistoryRepo.AddByCardId(cardId, history);
                logsByHistoryId[cardId] = GenerateReviewLogs(count: 5, historyId: cardId);
            }

            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, MakeBatchedFetcher(logsByHistoryId));
            var cardIds = Enumerable.Range(1, 10);

            var weights = await optimizer.OptimizeWeightsAsync(cardIds, epochs: 1, batchSize: 10);

            weights.Should().NotBeNull();
            weights.Length.Should().Be(21);
            weights.Should().NotContain(double.NaN);
            weights.Should().NotContain(double.NegativeInfinity).And.NotContain(double.PositiveInfinity);
        }

        [Fact]
        public async Task OptimizeWeightsAsync_BatchedFetcher_IsInvokedOnceWithAllHistoryIds()
        {
            // Verifies the N+1 fix: the delegate must receive one call containing ALL history ids,
            // not one call per id. This is the core architectural promise of the batched signature.
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            var logsByHistoryId = new Dictionary<int, List<ReviewLog>>();

            for (int cardId = 1; cardId <= 5; cardId++)
            {
                var history = new ReviewHistory { CardId = cardId, Id = cardId };
                reviewHistoryRepo.AddByCardId(cardId, history);
                logsByHistoryId[cardId] = GenerateReviewLogs(count: 3, historyId: cardId);
            }

            var fetchInvocations = new List<List<int>>();
            Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>> spyFetcher = ids =>
            {
                var idList = ids.ToList();
                fetchInvocations.Add(idList);
                IReadOnlyDictionary<int, List<ReviewLog>> dict = idList
                    .Where(id => logsByHistoryId.ContainsKey(id))
                    .ToDictionary(id => id, id => logsByHistoryId[id]);
                return Task.FromResult(dict);
            };

            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, spyFetcher);
            await optimizer.OptimizeWeightsAsync(Enumerable.Range(1, 5), epochs: 1, batchSize: 10);

            fetchInvocations.Should().HaveCount(1, "the batched fetcher must be called exactly once");
            fetchInvocations[0].Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public async Task OptimizeWeightsAsync_SkipsCardsWithNoHistory()
        {
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            var logsByHistoryId = new Dictionary<int, List<ReviewLog>>();

            var history = new ReviewHistory() { Id = 1 };
            reviewHistoryRepo.AddByCardId(1, history);
            logsByHistoryId[1] = GenerateReviewLogs(count: 5, historyId: 1);
            // cardId=2 deliberately not seeded

            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, MakeBatchedFetcher(logsByHistoryId));

            var weights = await optimizer.OptimizeWeightsAsync(new[] { 1, 2 }, epochs: 1, batchSize: 10);

            weights.Should().NotBeNull();
            weights.Length.Should().Be(21);
        }

        [Fact]
        public async Task OptimizeWeightsAsync_AllCardsInvalid_ThrowsArgumentException()
        {
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, MakeBatchedFetcher(new Dictionary<int, List<ReviewLog>>()));

            await optimizer.Awaiting(o => o.OptimizeWeightsAsync(new[] { 999, 1000 }, epochs: 1))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task OptimizeWeightsAsync_HistoriesLoadedButNoLogs_ThrowsInvalidOperationException()
        {
            // History exists but the delegate returns no matching logs - exercises the new
            // diagnostic safety check that fires before reaching the trainer.
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            var history = new ReviewHistory { CardId = 1, Id = 1 };
            reviewHistoryRepo.AddByCardId(1, history);

            // empty dictionary - delegate finds nothing
            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, MakeBatchedFetcher(new Dictionary<int, List<ReviewLog>>()));

            await optimizer.Awaiting(o => o.OptimizeWeightsAsync(new[] { 1 }, epochs: 1))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*no training samples could be generated*");
        }

        [Fact]
        public async Task OptimizeWeightsAsync_NullCardIds_ThrowsArgumentNullException()
        {
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, MakeBatchedFetcher(new Dictionary<int, List<ReviewLog>>()));

            await optimizer.Awaiting(o => o.OptimizeWeightsAsync(null!, epochs: 1))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task OptimizeWeightsAsync_WithoutRepositories_ThrowsInvalidOperationException()
        {
            var optimizer = new FsrsOptimizationService();

            await optimizer.Awaiting(o => o.OptimizeWeightsAsync(new[] { 1 }, epochs: 1))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public void OptimizationService_ConstructorRejectsNull_ReviewHistoryRepository()
        {
            var fetcher = MakeBatchedFetcher(new Dictionary<int, List<ReviewLog>>());

            var act = () => new FsrsOptimizationService(null!, fetcher);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void OptimizationService_ConstructorRejectsNull_BatchedFetcher()
        {
            var reviewHistoryRepo = new StubReviewHistoryRepository();

            var act = () => new FsrsOptimizationService(
                reviewHistoryRepo,
                (Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>>)null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void OptimizationService_ConstructorRejectsNull_PerIdFetcher()
        {
            var reviewHistoryRepo = new StubReviewHistoryRepository();

            var act = () => new FsrsOptimizationService(
                reviewHistoryRepo,
                (Func<int, Task<List<ReviewLog>>>)null!);
            act.Should().Throw<ArgumentNullException>();
        }

        // ---------------------------------------------------------
        // 5. Diagnostic API
        // ---------------------------------------------------------

        [Fact]
        public async Task OptimizeWeightsWithDiagnosticsAsync_ReturnsAccurateCounters()
        {
            var reviewHistoryRepo = new StubReviewHistoryRepository();
            var logsByHistoryId = new Dictionary<int, List<ReviewLog>>();

            for (int cardId = 1; cardId <= 4; cardId++)
            {
                var history = new ReviewHistory { CardId = cardId, Id = cardId };
                reviewHistoryRepo.AddByCardId(cardId, history);
                logsByHistoryId[cardId] = GenerateReviewLogs(count: 5, historyId: cardId);
            }
            // request 5 cards, only 4 exist - tests requested vs loaded gap

            var optimizer = new FsrsOptimizationService(reviewHistoryRepo, MakeBatchedFetcher(logsByHistoryId));

            var result = await optimizer.OptimizeWeightsWithDiagnosticsAsync(
                new[] { 1, 2, 3, 4, 999 }, epochs: 1, batchSize: 10);

            result.Weights.Should().HaveCount(21);
            result.HistoriesRequested.Should().Be(5);
            result.HistoriesLoaded.Should().Be(4);
            result.LogsLoaded.Should().Be(20); // 4 histories * 5 logs each
            result.SamplesGenerated.Should().BeGreaterThan(0);
        }

        // ---------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------

        // Wraps an in-memory dictionary as a batched fetcher delegate.
        private static Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>>
            MakeBatchedFetcher(Dictionary<int, List<ReviewLog>> source)
        {
            return ids =>
            {
                IReadOnlyDictionary<int, List<ReviewLog>> dict = ids
                    .Where(id => source.ContainsKey(id))
                    .ToDictionary(id => id, id => source[id]);
                return Task.FromResult(dict);
            };
        }

        private List<ReviewHistory> GenerateReviewHistories(int count)
        {
            var list = new List<ReviewHistory>();
            for (int i = 0; i < count; i++)
            {
                list.Add(CreateReviewHistory(cardId: i, reviewCount: 5));
            }
            return list;
        }

        private ReviewHistory CreateReviewHistory(int cardId, int reviewCount)
        {
            var history = new ReviewHistory { CardId = cardId };
            var rng = new Random(cardId);
            var now = DateTime.Now;

            for (int i = 0; i < reviewCount; i++)
            {
                var rating = (Rating)(rng.Next(1, 5));
                var reviewDate = now.AddDays(-reviewCount + i);
                var elapsedDays = (ulong)(i > 0 ? 1 : 0);

                var log = new ReviewLog
                {
                    Review = reviewDate,
                    Rating = rating,
                    ElapsedDays = elapsedDays
                };

                history.ReviewLogs.Add(log);
            }
            return history;
        }

        private List<ReviewLog> GenerateReviewLogs(int count, int historyId)
        {
            var logs = new List<ReviewLog>();
            var rng = new Random(historyId);
            var now = DateTime.Now;

            for (int i = 0; i < count; i++)
            {
                logs.Add(new ReviewLog
                {
                    Id = historyId * 100 + i,
                    Review = now.AddDays(-count + i),
                    Rating = (Rating)(rng.Next(1, 5)),
                    ElapsedDays = (ulong)(i > 0 ? 1 : 0),
                    ReviewHistoryId = historyId
                });
            }
            return logs;
        }

        // ---------------------------------------------------------
        // Stub history repository (in-memory, no DB)
        // ---------------------------------------------------------

        private class StubReviewHistoryRepository : IReviewHistoryRepository
        {
            private readonly Dictionary<int, ReviewHistory> _byId = new();
            private readonly Dictionary<int, ReviewHistory> _byCardId = new();

            public void AddByCardId(int cardId, ReviewHistory history)
            {
                _byCardId[cardId] = history;
                _byId[history.Id] = history;
            }

            public Task<ReviewHistory?> GetReviewHistoryByIdAsync(int reviewHistoryId)
            {
                _byId.TryGetValue(reviewHistoryId, out var result);
                return Task.FromResult(result);
            }

            public Task<ReviewHistory?> GetReviewHistoryByCardIdAsync(int cardId)
            {
                _byCardId.TryGetValue(cardId, out var result);
                return Task.FromResult(result);
            }

            public Task UpdateAsync(ReviewHistory reviewHistory) => Task.CompletedTask;
            public Task DeleteAsync(int reviewHistoryId) => Task.CompletedTask;
            public Task CreateAsync(ReviewHistory reviewHistory) => Task.CompletedTask;
        }
    }
}
