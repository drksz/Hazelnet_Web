using HazelNet_Application.CQRS.Abstractions;

namespace HazelNet_Application.CQRS.Features.Cards.Queries;

public record GetCountOfAllCardsQuery( int UserId): IQuery<int>;