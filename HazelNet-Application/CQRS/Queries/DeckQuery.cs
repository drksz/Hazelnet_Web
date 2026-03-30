using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;
using HazelNet_Application.CQRS.Abstractions;

namespace HazelNet_Application.CQRS.Queries;

public class GetDeckByIdQuery : IQuery<Deck>
{
    public int DeckId { get; set; }

    public GetDeckByIdQuery(int deckId)
    {
        DeckId = deckId;
    }
}

public class GetDeckByIdQueryHandler : IQueryHandler<GetDeckByIdQuery, Deck>
{
    private readonly IDeckRepository _deckRepository;

    public GetDeckByIdQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<Deck> Handle(GetDeckByIdQuery query)
    {
        var deck = await _deckRepository.Get(query.DeckId);
        return deck ?? throw new InvalidOperationException($"Deck with id {query.DeckId} not found.");
    }
}