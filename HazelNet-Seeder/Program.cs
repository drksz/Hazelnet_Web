using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is missing from appsettings.json");

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var db = new ApplicationDbContext(options);

// Idempotency check
var existing = await db.User
    .Include(u => u.Decks!)
        .ThenInclude(d => d.Cards)
            .ThenInclude(c => c.ReviewHistory)
                .ThenInclude(h => h.ReviewLogs)
    .FirstOrDefaultAsync(u => u.Username == "seed-user");

if (existing is not null)
{
    var deckCount = existing.Decks?.Count ?? 0;
    var cardCount = existing.Decks?.Sum(d => d.Cards.Count) ?? 0;
    var historyCount = existing.Decks?.Sum(d => d.Cards.Count(c => c.ReviewHistory is not null)) ?? 0;
    var logCount = existing.Decks?.Sum(d => d.Cards.Sum(c => c.ReviewHistory?.ReviewLogs.Count ?? 0)) ?? 0;

    Console.WriteLine($"seed-user already exists: {deckCount} decks, {cardCount} cards, {historyCount} histories, {logCount} review logs. Nothing to do.");
    return;
}

// --- Seed ---

var user = new User
{
    Username = "seed-user",
    EmailAddress = "seed@hazelnet.dev",
    PasswordHash = "not-a-real-hash"
};

db.User.Add(user);
await db.SaveChangesAsync();

string[] deckNames = ["Deck Alpha", "Deck Beta", "Deck Gamma"];
const int cardsPerDeck = 5;
const int logsPerHistory = 6;

var decks = new List<Deck>();
foreach (var name in deckNames)
{
    var deck = new Deck
    {
        DeckName = name,
        UserId = user.Id,
        User = user
    };
    decks.Add(deck);
    db.Decks.Add(deck);
}

await db.SaveChangesAsync();

// Ratings cycling: Again, Hard, Good, Easy, Good, Easy
Rating[] ratingCycle = [Rating.Again, Rating.Hard, Rating.Good, Rating.Easy, Rating.Good, Rating.Easy];

var baseDate = DateTime.UtcNow.Date.AddDays(-60);

var cards = new List<Card>();
int historyId = 1;
int logId = 1;

for (int di = 0; di < decks.Count; di++)
{
    var deck = decks[di];

    for (int ci = 0; ci < cardsPerDeck; ci++)
    {
        var card = new Card
        {
            FrontOfCard = $"Q{di + 1}-{ci + 1}",
            BackOfCard = $"A{di + 1}-{ci + 1}",
            DeckId = deck.Id,
            Deck = deck,
            State = State.Review,
            Due = DateTime.UtcNow.AddDays(1),
            LastReview = DateTime.UtcNow
        };

        db.Cards.Add(card);
        cards.Add(card);
    }
}

await db.SaveChangesAsync();

var histories = new List<ReviewHistory>();
var logs = new List<ReviewLog>();

foreach (var card in cards)
{
    var history = new ReviewHistory(card.Id) { Id = historyId++ };
    histories.Add(history);
    db.ReviewHistory.Add(history);

    // Spread 6 review logs across ~60 days
    // ElapsedDays increases: 0, 1, 3, 7, 14, 30
    ulong[] elapsedPattern = [0, 1, 3, 7, 14, 30];

    // Cumulative day offset from baseDate
    int dayOffset = 0;
    for (int li = 0; li < logsPerHistory; li++)
    {
        dayOffset += (int)elapsedPattern[li];

        var log = new ReviewLog
        {
            Id = logId++,
            ReviewHistoryId = history.Id,
            ReviewHistory = history,
            Rating = ratingCycle[li],
            ElapsedDays = elapsedPattern[li],
            ScheduledDays = elapsedPattern[li == logsPerHistory - 1 ? li : li + 1],
            Review = baseDate.AddDays(dayOffset),
            State = State.Review
        };

        logs.Add(log);
        db.ReviewLogs.Add(log);
    }
}

await db.SaveChangesAsync();

Console.WriteLine($"Seeded: 1 user, {decks.Count} decks, {cards.Count} cards, {histories.Count} histories, {logs.Count} review logs.");
