using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Cards.Queries;

public class GetAllCardsInDeckQueryHandler : IQueryHandler<GetAllCardsInDeckQuery, List<Card>>
{

    private readonly ICardRepository _cardRepository;
    
    public GetAllCardsInDeckQueryHandler(ICardRepository cardRepository)
    {
            _cardRepository = cardRepository;
    }

    public async Task<List<Card>> Handle(GetAllCardsInDeckQuery query)
    {
        return await _cardRepository.GetAllCardByDeckId(query.DeckId);
       
    }
}