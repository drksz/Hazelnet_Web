using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Application.CQRS.Features.ReviewLogs.Queries;
using HazelNet_Domain.IRepository;


namespace HazelNet_Application.CQRS.Features.Stats.Queries;

public class GetUserStatsQueryHandler : IQueryHandler<GetUserStatQuery, UserStatsViewModel>
{
    private readonly IStatsRepository _statsRepository;

    public GetUserStatsQueryHandler(IStatsRepository statsRepository)
    {
        _statsRepository = statsRepository;
    }
    
    public async Task<UserStatsViewModel> Handle(GetUserStatQuery query)
    {
        var result = await _statsRepository.GetUserStatsAsync(query.UserId);
        return result;
    }
}