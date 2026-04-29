using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Application.CQRS.Abstractions.Identity;
using HazelNet_Domain.IRepository;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;

public class DeleteCardCommandHandler :  ICommandHandler<DeleteCardCommand>
{
    private readonly ICardRepository  _cardRepository;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCardCommandHandler(ICardRepository cardRepository,IDeckRepository deckRepository ,ICurrentUserService currentUserService)
    {
        _cardRepository = cardRepository;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteCardCommand command)
    {
        var userIdString = await _currentUserService.GetUserIdAsync();
        
        if (!int.TryParse(userIdString, out var userId))
        {
            throw new Exception("User is not authenticated or token is invalid.");
        }
        var card = await _cardRepository.GetCardByIdAsync(command.CardId);
        if (card == null) throw new KeyNotFoundException("Card not found.");
        
        var deck = await _deckRepository.GetDeckByIdAsync(card.DeckId);
        
        if (deck == null || deck.UserId != userId)
        {
            throw new UnauthorizedAccessException("Not authorized to delete cards from this deck.");
        }

        // 3. Delete
        await _cardRepository.DeleteCardByIdAsync(command.CardId);
    }

    
}