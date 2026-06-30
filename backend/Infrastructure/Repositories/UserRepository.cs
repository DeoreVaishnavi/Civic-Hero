
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository(CivicHeroDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<bool> EmailExist(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}
