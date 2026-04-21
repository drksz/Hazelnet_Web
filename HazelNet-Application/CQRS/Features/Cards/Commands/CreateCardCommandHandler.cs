using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;

public class CreateCardCommandHandler : ICommandHandler<CreateCardCommand>
{
    
    private ICardRepository _cardRepository;
    private IReviewHistoryRepository _reviewHistoryRepository;

    public CreateCardCommandHandler(ICardRepository cardRepository, IReviewHistoryRepository reviewHistoryRepository)
    {
        _cardRepository = cardRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task Handle(CreateCardCommand command)
    {
        var reviewHistory = new ReviewHistory(command.Id);
        var card = new Card
        {
            FrontOfCard = command.Front,
            BackOfCard = command.Back,
            DeckId = command.DeckId,
            CreationDate = DateTime.UtcNow,
            State = State.New,
            LastReview = DateTime.UtcNow,
            Due = DateTime.UtcNow,
            ReviewHistory = reviewHistory
        };

        await _cardRepository.Create(card);
        await _reviewHistoryRepository.Create(reviewHistory);
    }
}