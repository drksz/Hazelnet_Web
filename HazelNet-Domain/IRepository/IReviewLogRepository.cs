using HazelNet_Domain.Models;


namespace HazelNet_Domain.IRepository;


public interface IReviewLogRepository
{
    Task<ReviewLog?> GetReviewLogByIdAsync(int reviewLogId);
    Task<List<ReviewLog>> GetReviewLogsByReviewHistoryIdAsync(int reviewHistoryId);
    Task UpdateAsync(ReviewLog reviewLog);
    Task DeleteAsync(int reviewLogId);
    Task CreateAsync(ReviewLog reviewLog);
}