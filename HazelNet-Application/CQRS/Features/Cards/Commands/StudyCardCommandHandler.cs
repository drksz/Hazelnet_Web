using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Services.FSRS;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;


public class StudyCardCommandHandler : ICommandHandler<StudyCardCommand>
{
    private ICardRepository _cardRepository;
    private IReviewLogRepository _reviewLogRepository;
    private IFSRSParametersRepository _fsrsParametersRepository;

    public StudyCardCommandHandler(ICardRepository cardRepository, IReviewLogRepository reviewLogRepository, IFSRSParametersRepository fsrsParametersRepository)
    {
        _cardRepository = cardRepository;
        _reviewLogRepository = reviewLogRepository;
        _fsrsParametersRepository = fsrsParametersRepository;
    }

    public async Task Handle(StudyCardCommand command)
    {
        var card = await _cardRepository.GetCardByIdAsync(command.CardId);
        if (card == null)
        {
            throw new Exception($"Card with Id {command.CardId} not found.");
        }

        var fsrsParameters = await _fsrsParametersRepository.GetFSRSParametersByUserIdAsync(command.userId);
        
        // Fall back to defaults if user has no custom parameters
        var parameters = fsrsParameters != null 
            ? new Parameters(fsrsParameters) 
            : Parameters.DefaultParam();
        
        var fsrs = new FSRS(parameters);
        var now = DateTime.UtcNow;
        
        var result = fsrs.Next(card, now, command.Rating);

        // Copy updated FSRS state back onto the tracked entity
        var updatedCard = result.Card;
        card.Due = updatedCard.Due;
        card.Stability = updatedCard.Stability;
        card.Difficulty = updatedCard.Difficulty;
        card.State = updatedCard.State;
        card.Reps = updatedCard.Reps;
        card.Lapses = updatedCard.Lapses;
        card.ElapsedDays = updatedCard.ElapsedDays;
        card.ScheduledDays = updatedCard.ScheduledDays;
        card.LastReview = updatedCard.LastReview;

        var reviewLog = result.ReviewLog;
        reviewLog.ReviewHistoryId = card.ReviewHistory.Id;

        // Both writes in one transaction
        await _reviewLogRepository.CreateAsync(reviewLog);
        await _cardRepository.UpdateAsync(card);
        
    }
}