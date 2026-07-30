using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<bool> EmailExist(string email);
        Task<User?> GetByEmail(string email);
        Task<List<User>> GetAllAsync();
        Task<User> AddAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(User user);
        Task<int> GetCountAsync();
    }
}