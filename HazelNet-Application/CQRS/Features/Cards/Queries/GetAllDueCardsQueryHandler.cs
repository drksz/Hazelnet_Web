using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Application.CQRS.Abstractions.Identity;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Cards.Queries;

public class GetAllDueCardsQueryHandler : IQueryHandler<GetAllDueCardsQuery, List<Card>>
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService  _currentUserService;
    
    public GetAllDueCardsQueryHandler(IDeckRepository deckRepository,ICurrentUserService  currentUserService)
    {
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<Card>> Handle(GetAllDueCardsQuery query)
    {
        var userIdString = await _currentUserService.GetUserIdAsync();
        
        if (!int.TryParse(userIdString, out var userId))
        {
            throw new Exception("User is not authenticated or token is invalid.");
        }

        var deck = await _deckRepository.GetDeckByIdAsync(query.DeckId);
     
        if (deck == null || deck.UserId != userId)
        {
            throw new UnauthorizedAccessException("Not authorized to delete cards from this deck.");
        }
        
        DateTime endOfToday = DateTime.UtcNow.Date.AddDays(1);
        
        var pendingCards = deck.Cards
            .Where(c => c.Due <= endOfToday)
            .OrderBy(c => c.Due)
            .ToList();
        
        return pendingCards;
    }
}