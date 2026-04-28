using HazelNet_Domain.Models;


namespace HazelNet_Domain.IRepository;

public interface IDeckRepository
{
   Task<Deck?> GetDeckByIdAsync(int deckId);
   Task<List<Deck>> GetAllDeckByUserIdAsync(int userId);
   Task UpdateDeckAsync(Deck deck);
   Task DeleteDeckAsync(Deck deck);
   Task CreateAsync(Deck deck);
   Task ClearAllCardsInDeckAsync(int deckId);
}