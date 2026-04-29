namespace HazelNet_Application.CQRS.Abstractions.Identity;

public interface ICurrentUserService
{
    Task<string?> GetUserIdAsync();
}