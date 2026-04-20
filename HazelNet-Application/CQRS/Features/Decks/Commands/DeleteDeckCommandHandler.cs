using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Application.CQRS.Abstractions.Identity;
using HazelNet_Domain.IRepository;

namespace HazelNet_Application.CQRS.Features.Decks.Commands;

public class DeleteDeckCommandHandler : ICommandHandler<DeleteDeckCommand>
{
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;
    
    public DeleteDeckCommandHandler
        (IDeckRepository deckRepository , 
            ICurrentUserService currentUserService)
    {
    _deckRepository = deckRepository;
    _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteDeckCommand command)
    {
        var userIdString = await _currentUserService.GetUserIdAsync();
        
        if (!int.TryParse(userIdString, out var userId))
        {
            throw new Exception("User is not authenticated or token is invalid.");
        }
        
       
        var deck = await _deckRepository.GetDeckByIdAsync(command.Id);
        if (deck is null)
            throw new Exception($"{nameof(deck)}, {command.Id} not found");
        
        if (deck.UserId != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this deck.");
        }

        await _deckRepository.DeleteDeckAsync(deck);
    }
}