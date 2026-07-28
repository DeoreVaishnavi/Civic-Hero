using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces
{
    public interface IComplaintService
    {
        Task<ComplaintDto?> CreateComplaintAsync(int userId, CreateComplaintDto dto);
        Task<ComplaintDto?> GetComplaintByIdAsync(int complaintId);
        Task<IEnumerable<ComplaintDto>> GetComplaintsByUserIdAsync(int userId);
        Task<IEnumerable<ComplaintDto>> GetAllComplaintsAsync();
        Task<ComplaintDto?> UpdateComplaintAsync(int complaintId, UpdateComplaintDto dto);
        Task<bool> DeleteComplaintAsync(int complaintId);
        Task<int> GetComplaintCountAsync();
    }
}