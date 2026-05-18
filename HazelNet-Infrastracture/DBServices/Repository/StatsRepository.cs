using HazelNet_Application.CQRS.Features.Stats;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;
using Microsoft.EntityFrameworkCore;

namespace HazelNet_Infrastractire.DBServices.Repository;

public class StatsRepository : IStatsRepository
{
    private readonly ApplicationDbContext _db;

    public StatsRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserStatsViewModel> GetUserStatsAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;

        var cards = await _db.Cards
            .Where(c => c.Deck.UserId == userId)
            .Select(c => new
            {
                c.State,
                c.Stability,
                c.ScheduledDays,
                c.Due,
                c.DeckId,
                DeckName = c.Deck.DeckName
            })
            .ToListAsync();

        var logs = await _db.ReviewLogs
            .Where(r => r.ReviewHistory.Card.Deck.UserId == userId)
            .Select(r => new { r.Rating, r.Review })
            .ToListAsync();

        var fsrsParams = await _db.FSRSParameters
            .FirstOrDefaultAsync(f => f.UserId == userId);

        // Streak — consecutive days with at least one review
        var reviewDates = logs
            .Select(r => r.Review.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int streak = 0;
        var expected = today;
        foreach (var date in reviewDates)
        {
            if (date == expected) { streak++; expected = expected.AddDays(-1); }
            else break;
        }

        // Daily reviews last 30 days
        var cutoff = today.AddDays(-29);
        var dailyReviews = logs
            .Where(r => r.Review.Date >= cutoff)
            .GroupBy(r => r.Review.Date)
            .Select(g => new DailyReviewStat(g.Key, g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        // Fill in zeroes for days with no reviews
        var allDays = Enumerable.Range(0, 30)
            .Select(i => cutoff.AddDays(i))
            .Select(d => dailyReviews.FirstOrDefault(r => r.Date == d) ?? new DailyReviewStat(d, 0))
            .ToList();

        // Per-deck stats
        var deckStats = cards
            .GroupBy(c => c.DeckName)
            .Select(g => new DeckStat(
                DeckName: g.Key,
                TotalCards: g.Count(),
                DueToday: g.Count(c => c.Due.Date <= today),
                MatureCards: g.Count(c => (ulong)c.ScheduledDays >= 21),
                AverageStability: g.Any() ? g.Average(c => c.Stability) : 0))
            .OrderBy(d => d.DeckName)
            .ToList();

        // Average retention — R = 0.9^(1/stability) approximation at scheduled interval
        var retentions = cards
            .Where(c => c.Stability > 0)
            .Select(c => Math.Pow(0.9, (double)c.ScheduledDays / c.Stability))
            .ToList();

        return new UserStatsViewModel
        {
            TotalDecks       = cards.Select(c => c.DeckId).Distinct().Count(),
            TotalCards       = cards.Count,
            TotalReviews     = logs.Count,
            ReviewsToday     = logs.Count(r => r.Review.Date == today),
            CurrentStreak    = streak,
            NewCards         = cards.Count(c => c.State == State.New),
            LearningCards    = cards.Count(c => c.State == State.Learning),
            ReviewCards      = cards.Count(c => c.State == State.Review),
            RelearningCards  = cards.Count(c => c.State == State.Relearning),
            AgainCount       = logs.Count(r => r.Rating == Rating.Again),
            HardCount        = logs.Count(r => r.Rating == Rating.Hard),
            GoodCount        = logs.Count(r => r.Rating == Rating.Good),
            EasyCount        = logs.Count(r => r.Rating == Rating.Easy),
            AverageStability = cards.Any() ? cards.Average(c => c.Stability) : 0,
            AverageRetention = retentions.Any() ? retentions.Average() * 100 : 0,
            HasCustomWeights = fsrsParams?.W != null,
            DailyReviews     = allDays,
            DeckStats        = deckStats
        };
    }
}