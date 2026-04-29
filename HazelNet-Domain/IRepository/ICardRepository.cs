using HazelNet_Domain.Models;

namespace HazelNet_Domain.IRepository;

public interface ICardRepository
{
    Task<Card?> GetCardByIdAsync(int cardId);
    Task<List<Card>> GetAllCardByDeckId(int deckId);
    Task UpdateAsync(Card card);
    Task DeleteAsync(int cardId);
    Task CreateAsync(Card card);
}