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