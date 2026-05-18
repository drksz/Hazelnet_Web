using HazelNet_Application.CQRS.Features.Stats;

namespace HazelNet_Domain.IRepository;

public interface IStatsRepository
{
    Task<UserStatsViewModel> GetUserStatsAsync(int userId);
}