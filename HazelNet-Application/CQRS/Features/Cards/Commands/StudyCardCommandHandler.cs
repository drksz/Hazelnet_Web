using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Services.FSRS;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;


public class StudyCardCommandHandler : ICommandHandler<StudyCardCommand>
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IReviewLogRepository _reviewLogRepository;
    private readonly IFSRSParametersRepository _fsrsParametersRepository;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public StudyCardCommandHandler(IDeckRepository deckRepository,ICardRepository cardRepository, IReviewLogRepository reviewLogRepository, IFSRSParametersRepository fsrsParametersRepository, IReviewHistoryRepository reviewHistoryRepository)
    {
        _deckRepository = deckRepository;
        _cardRepository = cardRepository;
        _reviewLogRepository = reviewLogRepository;
        _fsrsParametersRepository = fsrsParametersRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task Handle(StudyCardCommand command)
    {
        var deck = await _deckRepository.GetDeckByIdAsync(command.deckId);
        if (deck == null)
            throw new Exception("Deck not found");
        deck.LastAcess = DateTime.UtcNow;
        
        await _deckRepository.UpdateDeckAsync(deck);
        
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
        reviewLog.ReviewHistoryId = history.Id;

        // Both writes in one transaction
        await _reviewLogRepository.CreateAsync(reviewLog);
        await _cardRepository.UpdateAsync(card);
        
    }
}