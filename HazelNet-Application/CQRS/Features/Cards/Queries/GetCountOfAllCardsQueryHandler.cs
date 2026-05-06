using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Cards.Queries;

public class GetCountOfAllCardsQueryHandler : IQueryHandler<GetCountOfAllCardsQuery, int>
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICardRepository _cardRepository;

    public GetCountOfAllCardsQueryHandler(IDeckRepository deckRepository, ICardRepository  cardRepository)
    {
        _deckRepository = deckRepository;
        _cardRepository = cardRepository;
    }


    public async Task<int> Handle(GetCountOfAllCardsQuery query)
    {
        var DeckList = await _deckRepository.GetAllDeckByUserIdAsync(query.UserId);
        int cardCount = 0;

        foreach (var deck in DeckList)
        {
            var cardList = await _cardRepository.GetAllCardByDeckIdAsync(deck.Id);
            cardCount += cardList.Count;
        }
        
        return cardCount;
    }
}