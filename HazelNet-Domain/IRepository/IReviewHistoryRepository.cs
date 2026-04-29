using HazelNet_Domain.Models;


namespace HazelNet_Domain.IRepository;


public interface IReviewHistoryRepository
{
    Task<ReviewHistory?> GetReviewHistoryByIdAsync(int reviewHistoryId);
    Task<ReviewHistory?> GetReviewHistoryByCardIdAsync(int cardId);
    Task UpdateAsync(ReviewHistory reviewHistory);
    Task DeleteAsync(int reviewHistoryId);
    Task CreateAsync(ReviewHistory reviewHistory);
}