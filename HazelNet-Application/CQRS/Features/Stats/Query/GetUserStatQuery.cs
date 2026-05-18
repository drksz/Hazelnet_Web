using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Application.CQRS.Features.Stats;

namespace HazelNet_Application.CQRS.Features.ReviewLogs.Queries;

public record GetUserStatQuery(int UserId): IQuery<UserStatsViewModel>;