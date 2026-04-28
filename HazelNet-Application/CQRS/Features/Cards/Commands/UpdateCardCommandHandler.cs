using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Cards.Commands;

public class UpdateCardCommandHandler :  ICommandHandler<UpdateCardCommand>
{
    private readonly ICardRepository _cardRepository;

    public UpdateCardCommandHandler(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    public async Task Handle(UpdateCardCommand command)
    {
        var card = await _cardRepository.GetCardByIdAsync(command.Id);
        
        if (card == null)
            throw new NullReferenceException("Card not found");
        
         card.FrontOfCard = command.FrontOfCard;
         card.BackOfCard = command.BackOfCard;
         card.DeckId = command.DeckId;
         
         await _cardRepository.UpdateCardAsync(card);
        
    }
}