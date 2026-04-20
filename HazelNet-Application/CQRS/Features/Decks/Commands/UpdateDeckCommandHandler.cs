using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;

namespace HazelNet_Application.CQRS.Features.Decks.Commands;

public class UpdateDeckCommandHandler : ICommandHandler<UpdateDeckCommand>
{
    private readonly IDeckRepository _deckRepository;

    public UpdateDeckCommandHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task Handle(UpdateDeckCommand command)
    {
        var deck = await _deckRepository.GetDeckByIdAsync(command.DeckId);

        if (deck == null)
            throw new KeyNotFoundException("Deck not found");

        await _deckRepository.UpdateDeckAsync(deck);
    }
}