using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HazelNet_Infrastracture.DBServices.Repository;

//implementation of icardrepository 
public class CardRepository : ICardRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public CardRepository( IDbContextFactory<ApplicationDbContext> context)
    {
        _contextFactory = context;
    }
    
    public async Task<Card?> GetCardByIdAsync(int cardId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.Cards
            .Include(c => c.ReviewHistory)
            .FirstOrDefaultAsync(c => c.Id == cardId);
    }

    public async Task<List<Card>> GetAllCardByDeckIdAsync(int deckId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.Cards
            .Where(c => c.DeckId == deckId)
            .ToListAsync();
    }

    public async Task UpdateAsync(Card card)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.Cards.Update(card);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int cardId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        var card = await GetCardByIdAsync(cardId);
        if (card != null)
        {
            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();
        }
    }

    public async Task CreateAsync(Card card)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        await _context.Cards.AddAsync(card);
        await _context.SaveChangesAsync();
    }
}