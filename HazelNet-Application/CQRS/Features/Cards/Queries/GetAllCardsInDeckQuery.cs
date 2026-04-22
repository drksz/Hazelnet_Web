using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Cards.Queries;

public record GetAllCardsInDeckQuery (int DeckId)  :IQuery<List<Card>>;