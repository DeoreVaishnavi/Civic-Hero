using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Infrastructure.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly CivicHeroDbContext _context;

        public ComplaintRepository(CivicHeroDbContext context)
        {
            _context = context;
        }

        public async Task<Complaint?> GetByIdAsync(int complaintId)
        {
            return await _context.Complaints
                .Include(c => c.User)
                .Include(c => c.Ward)
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == complaintId);
        }

        public async Task<IEnumerable<Complaint>> GetByUserIdAsync(int userId)
        {
            return await _context.Complaints
                .Include(c => c.User)
                .Include(c => c.Ward)
                .Include(c => c.Department)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Complaint>> GetAllAsync()
        {
            return await _context.Complaints
                .Include(c => c.User)
                .Include(c => c.Ward)
                .Include(c => c.Department)
                .ToListAsync();
        }

        public async Task<Complaint> AddAsync(Complaint complaint)
        {
            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();
            return complaint;
        }

        public async Task<Complaint?> UpdateAsync(Complaint complaint)
        {
            var existing = await _context.Complaints.FindAsync(complaint.Id);
            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(complaint);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int complaintId)
        {
            var complaint = await _context.Complaints.FindAsync(complaintId);
            if (complaint == null)
                return false;

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetOpenComplaintCountAsync()
        {
            // Assuming Open means status not Resolved or Closed
            return await _context.Complaints.CountAsync(c => c.Status != "Resolved" && c.Status != "Closed");
        }

        public async Task<int> GetResolvedComplaintCountAsync()
        {
            // Assuming Resolved or Closed status
            return await _context.Complaints.CountAsync(c => c.Status == "Resolved" || c.Status == "Closed");
        }

        public async Task<int> GetComplaintsCreatedSinceAsync(DateTime startDate)
        {
            return await _context.Complaints.CountAsync(c => c.CreatedAt >= startDate);
        }
    }
}