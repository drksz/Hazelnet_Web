using HazelNet_Application.CQRS.Abstractions;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;

public record UpdateCardCommand(
    int Id,
    string FrontOfCard,
    string? BackOfCard,
    int DeckId
    ):ICommand;