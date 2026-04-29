using HazelNet_Application.CQRS.Abstractions;

namespace HazelNet_Application.CQRS.Features.Decks.Commands;

public record DeleteDeckCommand
(
    int Id
) : ICommand;
