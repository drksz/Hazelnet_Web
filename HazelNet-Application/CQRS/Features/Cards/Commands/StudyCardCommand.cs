using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;
using HazelNet_Domain.Services.FSRS;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;

public record StudyCardCommand
(
    FSRSParameters Parameters, //pass user's fsrs parameter here
    int CardId,
    Rating Rating
) : ICommand;
