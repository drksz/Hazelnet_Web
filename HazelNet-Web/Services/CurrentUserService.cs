using System.Security.Claims;
using HazelNet_Application.CQRS.Abstractions.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace HazelNet_Web.Services;

public class CurrentUserService :  ICurrentUserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public CurrentUserService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is not null && user.Identity.IsAuthenticated)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
        }

        return null;
    }
}