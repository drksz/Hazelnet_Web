using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;

namespace HazelNet_Infrastracture.DBServices;

public class CardService
{
    private readonly ApplicationDbContext _context;

    public CardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Card>> GetAllCardsAsync()
    {
        return await _context.Cards.ToListAsync();
    }

    public async Task<Card> GetCardByIdAsync(int cardId)
    {
        return await _context.Cards
            .FirstOrDefaultAsync(c => c.Id == cardId);
    }

    public async Task AddCardAsync(Card card)
    {
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCardAsync(Card card)
    {
        _context.Cards.Update(card);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCardAsync(int cardId)
    {
        var card = await _context.Cards.FindAsync(cardId);
        if (card != null)
        {
            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ReviewHistory> GetReviewHistoryByCardIdAsync(int cardId)
    {
        return await _context.ReviewHistory
            .Include(rh => rh.ReviewLogs) // Include related review logs
            .FirstOrDefaultAsync(rh => rh.CardId == cardId);
    }

}