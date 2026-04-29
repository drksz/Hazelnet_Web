using HazelNet_Domain.Models;


namespace HazelNet_Domain.IRepository;

public interface IFSRSParametersRepository
{
    Task<FSRSParameters?> GetFSRSParametersByIdAsync(int id);
    Task<FSRSParameters?> GetFSRSParametersByUserIdAsync(int userId);
    Task UpdateAsync(FSRSParameters parameters);
    Task CreateAsync(FSRSParameters parameters);
}