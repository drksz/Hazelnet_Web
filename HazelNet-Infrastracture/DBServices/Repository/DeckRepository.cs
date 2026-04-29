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
    private readonly ApplicationDbContext _context;

    public DeckRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Deck?> GetDeckByIdAsync(int deckId)
    {
        return await _context.Decks.FirstOrDefaultAsync(d => d.Id == deckId);
    }
    


    public async Task<List<Deck>> GetAllDeckByUserIdAsync(int userId)
    {
        return await _context.Decks
            .Where(d => d.UserId == userId)
            .ToListAsync();
    }

    public async Task UpdateDeckAsync(Deck deck)
    {
        _context.Decks.Update(deck);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDeckAsync(Deck _deck)
    {
       _context.Decks.Remove(_deck);
       await _context.SaveChangesAsync();
    }

    public async Task CreateAsync(Deck deck)
    {
         _context.Decks.Add(deck);
        await _context.SaveChangesAsync();
    }

    public async Task ClearAllCardsInDeckAsync(int deckId)
    {
        var cards = await _context.Cards.Where(c => c.DeckId == deckId).ExecuteDeleteAsync();
    }
}