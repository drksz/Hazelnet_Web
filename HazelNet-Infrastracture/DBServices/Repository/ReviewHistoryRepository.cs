using HazelNet_Domain.Models;
using HazelNet_Domain.IRepository;
using HazelNet_Infrastracture.DBContext;
using Microsoft.EntityFrameworkCore;

namespace HazelNet_Infrastracture.DBServices.Repository;

//implementation of ireviewhistoryrepository
public class ReviewHistoryRepository : IReviewHistoryRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ReviewHistoryRepository(IDbContextFactory<ApplicationDbContext> context)
    {
        _contextFactory = context;
    }

    public async Task<ReviewHistory?> GetReviewHistoryByIdAsync(int reviewHistoryId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.ReviewHistory.FindAsync(reviewHistoryId);
    }

    public async Task<ReviewHistory?> GetReviewHistoryByCardIdAsync(int cardId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.ReviewHistory
            .FirstOrDefaultAsync(rh => rh.CardId == cardId);
    }

    public async Task UpdateAsync(ReviewHistory reviewHistory)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.ReviewHistory.Update(reviewHistory);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int reviewHistoryId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        var reviewHistory = await GetReviewHistoryByIdAsync(reviewHistoryId);
        if (reviewHistory != null)
        {
            _context.ReviewHistory.Remove(reviewHistory);
            await _context.SaveChangesAsync();
        }
    }

    public async Task CreateAsync(ReviewHistory reviewHistory)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        await _context.ReviewHistory.AddAsync(reviewHistory);
        await _context.SaveChangesAsync();
    }
}
