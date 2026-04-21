using HazelNet_Application.CQRS.Abstractions;

namespace HazelNet_Application.CQRS.Features.Decks.Commands;

public record CreateDeckCommand
(
    string DeckName,
    string? DeckDescription
) : ICommand;