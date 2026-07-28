using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IComplaintRepository
    {
        Task<Complaint?> GetByIdAsync(int complaintId);
        Task<IEnumerable<Complaint>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Complaint>> GetAllAsync();
        Task<Complaint> AddAsync(Complaint complaint);
        Task<Complaint?> UpdateAsync(Complaint complaint);
        Task<bool> DeleteAsync(int complaintId);
        Task<int> GetCountAsync();
        Task<int> GetOpenComplaintCountAsync();
        Task<int> GetResolvedComplaintCountAsync();
        Task<int> GetComplaintsCreatedSinceAsync(DateTime startDate);
    }
}