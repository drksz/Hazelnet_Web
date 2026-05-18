namespace HazelNet_Application.CQRS.Features.Stats;

public class UserStatsViewModel
{
    // Overview
    public int TotalDecks { get; init; }
    public int TotalCards { get; init; }
    public int TotalReviews { get; init; }
    public int ReviewsToday { get; init; }
    public int CurrentStreak { get; init; }

    // Card state breakdown
    public int NewCards { get; init; }
    public int LearningCards { get; init; }
    public int ReviewCards { get; init; }
    public int RelearningCards { get; init; }

    // Rating distribution
    public int AgainCount { get; init; }
    public int HardCount { get; init; }
    public int GoodCount { get; init; }
    public int EasyCount { get; init; }

    // FSRS
    public double AverageStability { get; init; }
    public double AverageRetention { get; init; }
    public bool HasCustomWeights { get; init; }

    // Time series — last 30 days
    public List<DailyReviewStat> DailyReviews { get; init; } = new();

    // Per-deck breakdown
    public List<DeckStat> DeckStats { get; init; } = new();
}