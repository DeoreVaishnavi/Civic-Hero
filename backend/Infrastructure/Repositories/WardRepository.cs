using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public class WardRepository : GenericRepository<Ward>
{
    public WardRepository(CivicHeroDbContext context) : base(context)
    {
    }

    public async Task<Ward?> GetByCodeAsync(string code)
    {
        return await _context.Wards
            .FirstOrDefaultAsync(w => w.Code == code);
    }
}
