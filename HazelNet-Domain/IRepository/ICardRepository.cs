using HazelNet_Domain.Models;

namespace HazelNet_Domain.IRepository;

public interface ICardRepository
{
    Task<Card?> GetCardByIdAsync(int cardId);
    Task<List<Card>> GetAllCardByDeckId(int deckId);
    Task UpdateCardAsync(Card card);
    Task DeleteCardByIdAsync(int cardId);
    Task CreateCardAsync(Card card);
}