using HazelNet_Application.CQRS.Abstractions;

namespace HazelNet_Application.CQRS.Features.Decks.Queries;

public record GetCompletedDecksCountQuery(int UserId) : IQuery<int>;