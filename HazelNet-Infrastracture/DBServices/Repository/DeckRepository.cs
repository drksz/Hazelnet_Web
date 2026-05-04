using HazelNet_Domain.Models;
using HazelNet_Domain.IRepository;
using HazelNet_Infrastracture.DBContext;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HazelNet_Infrastracture.DBServices.Repository;

//implementation of ideckrepository
public class DeckRepository : IDeckRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public DeckRepository(IDbContextFactory<ApplicationDbContext> context)
    {
        _contextFactory = context;
    }

    public async Task<Deck?> GetDeckByIdAsync(int deckId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == deckId);
    }
    


    public async Task<List<Deck>> GetAllDeckByUserIdAsync(int userId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.Decks
            .Where(d => d.UserId == userId)
            .ToListAsync();
    }

    public async Task UpdateDeckAsync(Deck deck)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.Decks.Update(deck);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDeckAsync(Deck _deck)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
       _context.Decks.Remove(_deck);
       await _context.SaveChangesAsync();
    }

    public async Task CreateAsync(Deck deck)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
         _context.Decks.Add(deck);
        await _context.SaveChangesAsync();
    }

    public async Task ClearAllCardsInDeckAsync(int deckId)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        var cards = await _context.Cards.Where(c => c.DeckId == deckId).ExecuteDeleteAsync();
    }
}