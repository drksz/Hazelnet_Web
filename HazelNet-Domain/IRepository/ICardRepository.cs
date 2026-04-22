using HazelNet_Domain.Models;

namespace HazelNet_Domain.IRepository;

public interface ICardRepository
{
    Task<Card?> Get(int cardId);
    Task<List<Card>> GetAllCardByDeckId(int deckId);
    Task Update(Card card);
    Task Delete(int cardId);
    Task Create(Card card);
}