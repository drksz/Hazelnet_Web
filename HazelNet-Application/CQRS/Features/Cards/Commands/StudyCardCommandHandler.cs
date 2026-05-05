using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Services.FSRS;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;


public class StudyCardCommandHandler : ICommandHandler<StudyCardCommand>
{
    private ICardRepository _cardRepository;
    private IReviewLogRepository _reviewLogRepository;
    private IFSRSParametersRepository _fsrsParametersRepository;
    private IReviewHistoryRepository _reviewHistoryRepository;

    public StudyCardCommandHandler(ICardRepository cardRepository, IReviewLogRepository reviewLogRepository, IFSRSParametersRepository fsrsParametersRepository, IReviewHistoryRepository reviewHistoryRepository)
    {
        _cardRepository = cardRepository;
        _reviewLogRepository = reviewLogRepository;
        _fsrsParametersRepository = fsrsParametersRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task Handle(StudyCardCommand command)
    {
        var card = await _cardRepository.GetCardByIdAsync(command.CardId);
        if (card == null)
        {
            throw new Exception($"Card with Id {command.CardId} not found.");
        }


        var history = await _reviewHistoryRepository.GetReviewHistoryByIdAsync(card.ReviewHistory.Id);
        if (history == null )
        {
            throw new Exception($"Review history for card with Id {command.CardId} not found");
        }

        var fsrsParameters = await _fsrsParametersRepository.GetFSRSParametersByUserIdAsync(command.userId);
        
        // Fall back to defaults if user has no custom parameters
        var parameters = fsrsParameters != null 
            ? new Parameters(fsrsParameters) 
            : Parameters.DefaultParam();
        
        var fsrs = new FSRS(parameters);
        var now = DateTime.UtcNow;
        
        var result = fsrs.Next(card, now, command.Rating);
        card = result.Card;
        var reviewLog = result.ReviewLog;
        reviewLog.ReviewHistory = history;

        // Both writes in one transaction
        await _reviewLogRepository.CreateAsync(reviewLog);
        await _cardRepository.UpdateAsync(card);
        
    }
}