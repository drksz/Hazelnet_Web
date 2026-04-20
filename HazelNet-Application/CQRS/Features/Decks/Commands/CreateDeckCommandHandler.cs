using HazelNet_Application.CQRS.Abstractions;
using HazelNet_Application.CQRS.Abstractions.Identity;
using HazelNet_Application.Interface;
using HazelNet_Domain.IRepository;
using HazelNet_Domain.Models;

namespace HazelNet_Application.CQRS.Features.Decks.Commands;

public class CreateDeckCommandHandler : ICommandHandler<CreateDeckCommand, int>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDeckRepository _deckRepository;

    public CreateDeckCommandHandler(IDeckRepository deckRepository,  ICurrentUserService currentUserService)
    {
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }


    public async Task<int> Handle(CreateDeckCommand command)
    {
        var userIdString = await _currentUserService.GetUserIdAsync();
        
        if (!int.TryParse(userIdString, out var userId))
        {
            throw new Exception("User is not authenticated or token is invalid.");
        }
        
        var deck = new Deck
        {
            DeckName = command.DeckName,
            DeckDescription = command.DeckDescription,
            CreationDate = DateTime.UtcNow,
            LastAcess = DateTime.UtcNow,
            UserId = userId
        };
        
        await _deckRepository.CreateAsync(deck);
        return deck.Id;
    }
}