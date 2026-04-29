using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.ReviewLogs.Queries;

public record GetReviewLogsByCardIdQuery(int CardId) : IQuery<IEnumerable<ReviewLog>>;

public class GetReviewLogsByCardIdQueryHandler : IQueryHandler<GetReviewLogsByCardIdQuery, IEnumerable<ReviewLog>>
{
    private readonly IReviewLogRepository _reviewLogRepository;
    private readonly ICardRepository _cardRepository;

    public GetReviewLogsByCardIdQueryHandler(IReviewLogRepository reviewLogRepository, ICardRepository cardRepository)
    {
        _reviewLogRepository = reviewLogRepository;
        _cardRepository = cardRepository;
    }

    public async Task<IEnumerable<ReviewLog>> Handle(GetReviewLogsByCardIdQuery query)
    {
        var card = await _cardRepository.GetCardByIdAsync(query.CardId);
        if (card == null)
        {
            throw new Exception($"Card with Id {query.CardId} not found.");
        }
        var reviewHistory = card.ReviewHistory;
        if (reviewHistory == null)
        {
            throw new Exception($"ReviewHistory for Card with Id {query.CardId} not found.");
        }
        var reviewLogs = await _reviewLogRepository.GetReviewLogsByReviewHistoryIdAsync(reviewHistory.Id);
        return reviewLogs;
    }
}