using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Decks.Queries;

public class GetDecksVMQueryHandler 
    : IQueryHandler<GetDecksVMQuery, List<DeckViewModel>>
{
    private readonly IDeckRepository _deckRepository;
    public GetDecksVMQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }
        
    public async Task<List<DeckViewModel>> Handle(GetDecksVMQuery query)
    {

        var decks = await _deckRepository.GetAllDeckByUserIdAsync(query.UserId);
        DateTime now = DateTime.UtcNow;

        var result = decks.Select(d => new DeckViewModel
        {
            Id = d.Id,
            Name = d.DeckName,
            Description = d.DeckDescription,
            TotalNumberOfCards = d.Cards.Count,
            LastDateAccessed = d.LastAcess,
            CreationDate = d.CreationDate,
            DueToday = d.Cards.Count(c => c.State == State.New || c.Due <= now),
            MasteredCards = d.Cards.Count(c => c.State == State.Review && c.Due > now),
            EarliestDueDate = d.Cards
            .Where(c => c.State != State.New)
            .MinBy(c => c.Due)?.Due
        }).ToList();

        return result;
    }
}

