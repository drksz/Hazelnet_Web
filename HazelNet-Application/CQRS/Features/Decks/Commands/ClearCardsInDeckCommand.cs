using HazelNet_Application.CQRS.Abstractions;

namespace HazelNet_Application.CQRS.Features.Decks.Commands;

public record ClearCardsInDeckCommand(int Id): ICommand;