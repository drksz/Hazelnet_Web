using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Decks.Queries;

public class GetCompletedDecksCountQueryHandler : IQueryHandler<GetCompletedDecksCountQuery, int>
{
    private readonly IDeckRepository _deckRepository;

    public GetCompletedDecksCountQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<int> Handle(GetCompletedDecksCountQuery query)
    {
        var decks = await _deckRepository.GetAllDeckByUserIdAsync(query.UserId);

        DateTime now = DateTime.UtcNow;

        int completedCount = decks.Count(d =>
            d.Cards.Any() &&
            d.Cards.All(c => c.State != State.New && c.Due > now));

        return completedCount;
    }   
}