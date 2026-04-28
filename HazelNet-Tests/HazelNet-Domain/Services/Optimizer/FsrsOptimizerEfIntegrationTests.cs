using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HazelNet.Services.Optimizer;
using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;
using HazelNet_Infrastracture.DBServices.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HazelNet.Tests.Optimizer
{
    /// <summary>
    /// Integration tests that exercise the full optimizer pipeline against a rea
    /// EF Core stack. We use SQLite in-memory mode (not the EF InMemory provider)
    /// because SQLite enforces foreign keys, applies real query translation, and
    /// generally surfaces the same class of bugs you'd hit against Postgres.
    ///
    /// These tests prove three things the pure-stub tests cannot:
    ///   1. The injected delegate works when wired to real EF Core IQueryable.
    ///   2. ReviewHistoryRepository.GetReviewHistoryByCardId returns hydrated
    ///      enough state for the optimizer (specifically, the Id property).
    ///   3. The full sequence: history lookup -> batched logs query ->
    ///      processor -> trainer -> weights produces valid output on data
    ///      that has actually been persisted and re-read.
    /// </summary>
    public class FsrsOptimizerEfIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public FsrsOptimizerEfIntegrationTests()
        {
            // SQLite in-memory only lives as long as the connection is open.
            // We hold the connection at the test-class level so all DbContexts
            // created during a single test see the same database.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var ctx = new ApplicationDbContext(_options);
            ctx.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        [Fact]
        public async Task EndToEnd_OptimizeWeightsAsync_ReadsFromRealDb_AndTrains()
        {
            // Arrange: seed a deck, cards, histories, and a realistic spread of review logs.
            const int cardCount = 8;
            const int logsPerCard = 6;

            List<int> seededCardIds;
            using (var seedCtx = new ApplicationDbContext(_options))
            {
                seededCardIds = SeedDeckCardsAndHistories(seedCtx, cardCount, logsPerCard);
            }

            // Wire up the optimizer with the real repository plus an EF-backed delegate.
            // The delegate here is exactly what production code is expected to pass:
            // a single batched WHERE...IN query followed by a GroupBy.
            using var ctx = new ApplicationDbContext(_options);
            var historyRepo = new ReviewHistoryRepository(ctx);

            Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>> logFetcher = async historyIds =>
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
            var result = await optimizer.OptimizeWeightsWithDiagnosticsAsync(seededCardIds, epochs: 1, batchSize: 16);

            // Assert: data flowed end-to-end, all counters match what we seeded.
            result.HistoriesRequested.Should().Be(cardCount);
            result.HistoriesLoaded.Should().Be(cardCount);
            result.LogsLoaded.Should().Be(cardCount * logsPerCard);
            result.SamplesGenerated.Should().BeGreaterThan(0);

            // Weights are well-formed and didn't blow up numerically.
            result.Weights.Should().HaveCount(21);
            result.Weights.Should().NotContain(double.NaN);
            result.Weights.Should().NotContain(double.NegativeInfinity).And.NotContain(double.PositiveInfinity);
            result.Weights[0].Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task EndToEnd_BatchedFetcher_IssuesSingleQueryAcrossAllHistories()
        {
            // The delegate is the contract that solves the N+1 problem. We can't directly
            // count SQL statements without a logger interceptor, but we can prove the
            // batched method gets all histories in one dictionary lookup.
            const int cardCount = 5;

            List<int> seededCardIds;
            using (var seedCtx = new ApplicationDbContext(_options))
            {
                seededCardIds = SeedDeckCardsAndHistories(seedCtx, cardCount, logsPerCard: 4);
            }

            using var ctx = new ApplicationDbContext(_options);
            var historyRepo = new ReviewHistoryRepository(ctx);

            int fetcherCallCount = 0;
            Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>> logFetcher = async historyIds =>
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

            await optimizer.OptimizeWeightsAsync(seededCardIds, epochs: 1, batchSize: 16);

            fetcherCallCount.Should().Be(1, "the batched fetcher must be invoked exactly once for all histories");
        }

        [Fact]
        public async Task EndToEnd_PerIdConvenienceOverload_AlsoWorksAgainstRealDb()
        {
            // The convenience overload (per-id delegate) is slower but should still produce
            // valid output. Useful for callers who already have a method-group fetcher.
            const int cardCount = 4;

            List<int> seededCardIds;
            using (var seedCtx = new ApplicationDbContext(_options))
            {
                seededCardIds = SeedDeckCardsAndHistories(seedCtx, cardCount, logsPerCard: 5);
            }

            using var ctx = new ApplicationDbContext(_options);
            var historyRepo = new ReviewHistoryRepository(ctx);

            // Per-id delegate, naive style - one query per id.
            Func<int, Task<List<ReviewLog>>> perIdFetcher = async historyId =>
                await ctx.ReviewLogs
                    .Where(r => r.ReviewHistoryId == historyId)
                    .ToListAsync();

            var optimizer = new FsrsOptimizationService(historyRepo, perIdFetcher);

            var weights = await optimizer.OptimizeWeightsAsync(seededCardIds, epochs: 1, batchSize: 16);

            weights.Should().HaveCount(21);
            weights.Should().NotContain(double.NaN);
        }

        [Fact]
        public async Task EndToEnd_PartialMatch_LoadsOnlyExistingHistories()
        {
            // Mix valid and invalid card ids - the optimizer should silently skip the missing ones
            // and still train successfully on the rest.
            List<int> seededCardIds;
            using (var seedCtx = new ApplicationDbContext(_options))
            {
                seededCardIds = SeedDeckCardsAndHistories(seedCtx, cardCount: 3, logsPerCard: 4);
            }

            using var ctx = new ApplicationDbContext(_options);
            var historyRepo = new ReviewHistoryRepository(ctx);

            Func<IEnumerable<int>, Task<IReadOnlyDictionary<int, List<ReviewLog>>>> logFetcher = async historyIds =>
            {
                var idList = historyIds.ToList();
                var logs = await ctx.ReviewLogs.Where(r => idList.Contains(r.ReviewHistoryId)).ToListAsync();
                IReadOnlyDictionary<int, List<ReviewLog>> dict =
                    logs.GroupBy(r => r.ReviewHistoryId).ToDictionary(g => g.Key, g => g.ToList());
                return dict;
            };

            var optimizer = new FsrsOptimizationService(historyRepo, logFetcher);

            // Real seeded card IDs plus two non-existent ones.
            var idsToRequest = seededCardIds.Concat(new[] { 999, 1000 }).ToList();

            var result = await optimizer.OptimizeWeightsWithDiagnosticsAsync(
                idsToRequest, epochs: 1, batchSize: 16);

            result.HistoriesRequested.Should().Be(seededCardIds.Count + 2);
            result.HistoriesLoaded.Should().Be(seededCardIds.Count);
            result.LogsLoaded.Should().Be(seededCardIds.Count * 4);
            result.Weights.Should().HaveCount(21);
        }

        // ---------------------------------------------------------
        // Seeding helpers
        // ---------------------------------------------------------

        // Returns the list of card IDs that were actually generated by EF/SQLite, so tests
        // can pass them straight to the optimizer instead of guessing at a 1..N range.
        private static List<int> SeedDeckCardsAndHistories(ApplicationDbContext ctx, int cardCount, int logsPerCard)
        {
            // Build the dependency chain bottom-up: User -> Deck -> Card -> ReviewHistory -> ReviewLog.
            // Deck requires a UserId FK and Card requires a DeckId FK, so a User must exist first.
            // User.Id and Deck.Id are ValueGeneratedOnAdd; we let EF assign them and use the
            // navigation properties to wire FKs implicitly.
            var user = new User
            {
                Username = "test-user",
                EmailAddress = "test@example.com",
                PasswordHash = "not-a-real-hash"
            };
            ctx.User.Add(user);
            ctx.SaveChanges();

            var deck = new Deck
            {
                DeckName = "TestDeck",
                UserId = user.Id
            };
            ctx.Decks.Add(deck);
            ctx.SaveChanges();

            // Card.Id is private-set + ValueGeneratedOnAdd, so EF generates IDs at SaveChanges.
            // We add cards then save, then re-read to get the assigned IDs.
            for (int i = 1; i <= cardCount; i++)
            {
                var card = new Card
                {
                    FrontOfCard = $"front-{i}",
                    BackOfCard = $"back-{i}",
                    DeckId = deck.Id
                };
                ctx.Cards.Add(card);
            }
            ctx.SaveChanges();

            // After save, cards have generated Ids. Re-read them so we know the actual values
            // and can attach histories with matching CardId.
            var savedCards = ctx.Cards.OrderBy(c => c.Id).ToList();

            int historyId = 1;
            int logIdSeed = 1;
            var rng = new Random(42);
            var now = DateTime.UtcNow;

            foreach (var card in savedCards)
            {
                var history = new ReviewHistory(card.Id) { Id = historyId };
                ctx.ReviewHistory.Add(history);

                for (int j = 0; j < logsPerCard; j++)
                {
                    var log = new ReviewLog
                    {
                        Id = logIdSeed++,
                        Review = now.AddDays(-logsPerCard + j),
                        Rating = (Rating)(rng.Next(1, 5)),
                        ElapsedDays = (ulong)(j > 0 ? rng.Next(1, 10) : 0),
                        ScheduledDays = (ulong)rng.Next(1, 15),
                        State = State.Review,
                        ReviewHistoryId = history.Id
                    };
                    ctx.ReviewLogs.Add(log);
                }
                historyId++;
            }
            ctx.SaveChanges();

            return savedCards.Select(c => c.Id).ToList();
        }
    }
}
