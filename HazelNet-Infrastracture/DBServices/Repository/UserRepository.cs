using HazelNet_Application.Interface;
using HazelNet_Domain.Models;
using HazelNet_Infrastracture.DBContext;
using Microsoft.EntityFrameworkCore;

namespace HazelNet_Infrastracture.Command;

public class UserRepository :  IUserRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    public UserRepository(IDbContextFactory<ApplicationDbContext> context)
    {
        _contextFactory = context;
    }

    public async Task<bool> EmailExistAsync(string email)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.User.AnyAsync(c => c.EmailAddress == email);
    }

    public async Task RegisterUserAsync(User user)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        _context.User.Add(user);
        await _context.SaveChangesAsync();
    }

    //Made it nullable for possiblity of null
    public async Task<string?> GetPasswordHashAsync(string email)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.User
            .Where(c => c.EmailAddress == email)
            .Select(c => c.PasswordHash)
            .FirstOrDefaultAsync();
    }

    //Made a new query for getting user by email
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        await using var _context = await _contextFactory.CreateDbContextAsync();
        return await _context.User
            .Where(c => c.EmailAddress == email)
            .FirstOrDefaultAsync();
    }
}