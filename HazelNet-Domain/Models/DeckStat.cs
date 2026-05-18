namespace HazelNet_Application.CQRS.Features.Stats;

public record DeckStat(
    string DeckName,
    int TotalCards,
    int DueToday,
    int MatureCards,   // ScheduledDays >= 21
    double AverageStability);