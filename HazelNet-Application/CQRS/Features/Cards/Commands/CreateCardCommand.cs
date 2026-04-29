using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;

public record CreateCardCommand
(
    int Id,
    string Front,
    string Back,
    int DeckId
) : ICommand;
