using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.ReviewLogs.Queries;

public record GetReviewLogsByIdQuery(int Id) : IQuery<ReviewLog>;

public class GetReviewLogsByIdQueryHandler : IQueryHandler<GetReviewLogsByIdQuery, ReviewLog>
{
    private readonly IReviewLogRepository _reviewLogRepository;

    public GetReviewLogsByIdQueryHandler(IReviewLogRepository reviewLogRepository)
    {
        _reviewLogRepository = reviewLogRepository;
    }

    public async Task<ReviewLog> Handle(GetReviewLogsByIdQuery query)
    {
        var reviewLog = await _reviewLogRepository.Get(query.Id);
        return reviewLog ?? throw new Exception($"ReviewLog with Id {query.Id} not found.");
    }
}