using FluentAssertions;
using HazelNet_Application.CQRS.Features.Cards.Commands;
using HazelNet_Application.CQRS.Abstractions.Identity;
using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;
using HazelNet_Infrastracture.DBServices.Repository;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;


/*
 *  TESTING STRATEGY
 *
 * For the DB integration tests, we use an in-memory SQLite DB connection to test our handlers.
 *
 * The assumption is that if the handlers' logic works perfectly against EFCore SQLite, then we can be confident
 * and assume that it should work the same way against EFCore postgreSQL provider (since EFCore translates
 * our linq queries into SQL).
 *
 * This is also a zero-setup approach since we do not need to have a live postgreSQL server containing a
 * test container each time we run the tests.
 */


namespace HazelNet_Tests.HazelNet_Application.CQRS.Features.Cards.Commands;



public class CardCommandsIntegrationTests : IDisposable
{
    
    // Replace the field and constructor
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public CardCommandsIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>() // ✅ not IDbContextFactory
            .UseSqlite(_connection)
            .Options;

        _dbContextFactory = new TestDbContextFactory(options);

        // Ensure schema is created once
        using var ctx = _dbContextFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
    }
    

    [Fact]
    public async Task CreateCardCommandHandler_ShouldAddCardToDatabase()
    {
        // arrange
            // create concrete implementations of the repositories
            
        await using var ctx = _dbContextFactory.CreateDbContext();
        
        var cardRepository = new CardRepository(_dbContextFactory);
        var reviewHistoryRepository = new ReviewHistoryRepository(_dbContextFactory);
        
        var handler = new CreateCardCommandHandler(cardRepository, reviewHistoryRepository);
        
        var user = new User { Username = "TestUser", EmailAddress = "test@example.com", PasswordHash = "hash" };
        ctx.User.Add(user);
        await ctx.SaveChangesAsync();

        var deck = new Deck { DeckName = "Test Deck", CreationDate = DateTime.UtcNow, UserId = user.Id };
        ctx.Decks.Add(deck);
        await ctx.SaveChangesAsync();

            // creating a command to test
        var command = new CreateCardCommand(
            Id: 0, 
            Front: "What is CQRS?",
            Back: "Command Query Responsibility Segregation",
            DeckId: deck.Id
        );

        // act
        await handler.Handle(command);

        // assert
            // We query the DB directly to ensure the command successfully mutated the state!
        var savedCard = await ctx.Cards.FirstOrDefaultAsync(c => c.FrontOfCard == "What is CQRS?");
        
        savedCard.Should().NotBeNull();
        savedCard!.BackOfCard.Should().Be("Command Query Responsibility Segregation");
        savedCard.DeckId.Should().Be(deck.Id);
    }

    [Fact]
    public async Task DeleteCardCommandHandler_ShouldRemoveCardFromDatabase()
    {
        await using var ctx = _dbContextFactory.CreateDbContext();

        // arrange
        var user = new User { Username = "TestUser", EmailAddress = "test@example.com", PasswordHash = "hash" };
        ctx.User.Add(user);
        await ctx.SaveChangesAsync();

        var deck = new Deck { DeckName = "Test Deck", CreationDate = DateTime.UtcNow, UserId = user.Id };
        ctx.Decks.Add(deck);
        await ctx.SaveChangesAsync();

        var card = new Card
        {
            FrontOfCard = "To Delete", 
            DeckId = deck.Id, Deck = null!, 
            State = State.New, 
            CreationDate = DateTime.UtcNow, 
            LastReview = DateTime.UtcNow, 
            Due = DateTime.UtcNow, 
            ReviewHistory = new ReviewHistory()
        };
        
        ctx.Cards.Add(card);
        await ctx.SaveChangesAsync();

        var cardRepository = new CardRepository(_dbContextFactory);
        var deckRepository = new DeckRepository(_dbContextFactory);
        var currentUserService = new MockCurrentUserService(user.Id.ToString());
        
        var handler = new DeleteCardCommandHandler(cardRepository, deckRepository, currentUserService);
        var command = new DeleteCardCommand(card.Id);

        // act
        await handler.Handle(command);

        // assert
        var deletedCard = await ctx.Cards.FindAsync(card.Id);
        deletedCard.Should().BeNull();
    }

    public void Dispose()
    {
        // clean up connections after each test finishes
        _connection.Dispose();
    }
    
    public class MockCurrentUserService : ICurrentUserService
    {
        private readonly string _userId;
        public MockCurrentUserService(string userId) => _userId = userId;
        public Task<string?> GetUserIdAsync() => Task.FromResult<string?>(_userId);
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
    
    
    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;
        public ApplicationDbContext CreateDbContext() => new SqliteTestDbContext(_options);
    }
}
