using HazelNet_Application.CQRS.Abstractions;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;

public record DeleteCardCommand
    (
        int CardId
    ): ICommand;