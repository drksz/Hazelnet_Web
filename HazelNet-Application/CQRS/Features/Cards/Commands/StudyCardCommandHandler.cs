using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Services.FSRS;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;


public class StudyCardCommandHandler : ICommandHandler<StudyCardCommand>
{
    private ICardRepository _cardRepository;
    private IReviewLogRepository _reviewLogRepository;

    public StudyCardCommandHandler(ICardRepository cardRepository, IReviewLogRepository reviewLogRepository)
    {
        _cardRepository = cardRepository;
        _reviewLogRepository = reviewLogRepository;
    }

    public async Task Handle(StudyCardCommand command)
    {
        var card = await _cardRepository.GetCardByIdAsync(command.CardId);
        if (card == null)
        {
            throw new Exception($"Card with Id {command.CardId} not found.");
        }
        
        var parameters = new Parameters();
        var fsrs = new FSRS(parameters);
        var now = DateTime.UtcNow;

        var result = fsrs.Next(card, now, command.Rating);
        var reviewLog = result.ReviewLog;
        reviewLog.ReviewHistoryId = card.ReviewHistory.Id;

        await _reviewLogRepository.Create(reviewLog);
        await _cardRepository.UpdateCardAsync(card);

    }
}