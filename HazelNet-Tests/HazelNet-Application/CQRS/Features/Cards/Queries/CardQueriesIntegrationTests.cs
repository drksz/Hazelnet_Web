using FluentAssertions;
using HazelNet_Application.CQRS.Features.Cards.Queries;
using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;
using HazelNet_Infrastracture.DBServices.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HazelNet_Tests.HazelNet_Application.CQRS.Features.Cards.Queries;

public class CardQueriesIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public CardQueriesIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new SqliteTestDbContext(options);
        _dbContext.Database.EnsureCreated();

        _contextFactory = new TestDbContextFactory(options);
    }

    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext()
        {
            return new SqliteTestDbContext(_options);
        }
    }

    [Fact]
    public async Task GetCardsByDeckIdQueryHandler_ShouldReturnCardsForSpecificDeck()
    {
        // arrange
        var cardRepository = new CardRepository(_contextFactory);
        var handler = new GetCardsByDeckIdQueryHandler(cardRepository);

        var user = new User { Username = "TestUser", EmailAddress = "test@example.com", PasswordHash = "hash" };
        _dbContext.User.Add(user);
        await _dbContext.SaveChangesAsync();

        var deck1 = new Deck { DeckName = "Deck 1", CreationDate = DateTime.UtcNow, UserId = user.Id };
        var deck2 = new Deck { DeckName = "Deck 2", CreationDate = DateTime.UtcNow, UserId = user.Id };
        _dbContext.Decks.AddRange(deck1, deck2);
        await _dbContext.SaveChangesAsync();

        _dbContext.Cards.AddRange(
            new Card
            {
                FrontOfCard = "Q1", 
                BackOfCard = "A1", 
                DeckId = deck1.Id, 
                Deck = null!, 
                State = State.New, 
                CreationDate = DateTime.UtcNow, 
                LastReview = DateTime.UtcNow, 
                Due = DateTime.UtcNow, 
                ReviewHistory = new ReviewHistory()
            },
            new Card
            {
                FrontOfCard = "Q2", 
                BackOfCard = "A2", 
                DeckId = deck1.Id, 
                Deck = null!, 
                State = State.New, 
                CreationDate = DateTime.UtcNow, 
                LastReview = DateTime.UtcNow, 
                Due = DateTime.UtcNow, 
                ReviewHistory = new ReviewHistory()
            },
            new Card
            {
                FrontOfCard = "Q3", 
                BackOfCard = "A3", 
                DeckId = deck2.Id, 
                Deck = null!, 
                State = State.New, 
                CreationDate = DateTime.UtcNow, 
                LastReview = DateTime.UtcNow, 
                Due = DateTime.UtcNow, 
                ReviewHistory = new ReviewHistory()
            }
        );
        await _dbContext.SaveChangesAsync();

        var query = new GetCardsByDeckIdQuery(deck1.Id);

        // act
        var result = await handler.Handle(query);

        // assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(c => c.DeckId == deck1.Id).Should().BeTrue();
    }
    
    
    [Fact]
    public async Task GetCardByIdQueryHandler_ShouldReturnCorrectCard()
    {
        // arrange
        var cardRepository = new CardRepository(_contextFactory);
        var handler = new GetCardByIdQueryHandler(cardRepository);

        var user = new User { Username = "TestUser", EmailAddress = "test@example.com", PasswordHash = "hash" };
        _dbContext.User.Add(user);
        await _dbContext.SaveChangesAsync();

        var deck = new Deck { DeckName = "Deck 1", CreationDate = DateTime.UtcNow, UserId = user.Id };
        _dbContext.Decks.Add(deck);
        await _dbContext.SaveChangesAsync();

        var card = new Card
        {
            FrontOfCard = "Q1", 
            BackOfCard = "A1", 
            DeckId = deck.Id, 
            Deck = null!, 
            State = State.New, 
            CreationDate = DateTime.UtcNow, 
            LastReview = DateTime.UtcNow, 
            Due = DateTime.UtcNow, 
            ReviewHistory = new ReviewHistory()
        };
        _dbContext.Cards.Add(card);
        await _dbContext.SaveChangesAsync();

        var query = new GetCardByIdQuery(card.Id);

        // act
        var result = await handler.Handle(query);

        // assert
        result.Should().NotBeNull();
        result.Id.Should().Be(card.Id);
        result.FrontOfCard.Should().Be("Q1");
        result.BackOfCard.Should().Be("A1");
    }
    

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    // PR #82 added User.FSRSParameters (double[] W — Npgsql-specific, no SQLite value converter)
    // and Card.ReviewHistoryId (shadows the explicit 1:1 FK config). Both are ignored here so
    // EF Core can build a valid model against SQLite's in-memory store.
    private sealed class SqliteTestDbContext : ApplicationDbContext
    {
        public SqliteTestDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>().Ignore(u => u.FSRSParameters);
            modelBuilder.Entity<Card>().Ignore(c => c.ReviewHistoryId);
        }
    }
}
