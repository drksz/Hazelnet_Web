using HazelNet_Domain.Models;
using HazelNet_Domain.IRepository;
using HazelNet_Infrastracture.DBContext;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HazelNet_Infrastracture.DBServices.Repository;

public class FSRSParametersRepository : IFSRSParametersRepository
{
    private readonly ApplicationDbContext _context;

    public FSRSParametersRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FSRSParameters?> GetFSRSParametersByIdAsync(int id)
    {
        return await _context.FSRSParameters.FindAsync(id);
    }

    public async Task<FSRSParameters?> GetFSRSParametersByUserIdAsync(int userId)
    {
        return await _context.FSRSParameters.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task UpdateAsync(FSRSParameters parameters)
    {
        _context.FSRSParameters.Update(parameters);
        await _context.SaveChangesAsync();
    }

    public async Task CreateAsync(FSRSParameters parameters)
    {
        await _context.FSRSParameters.AddAsync(parameters);
        await _context.SaveChangesAsync();
     }
}