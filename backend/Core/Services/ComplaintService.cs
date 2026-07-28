using AutoMapper;
using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using CivicHero.Backend.Infrastructure.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepository;
        private readonly IMapper _mapper;

        public ComplaintService(IComplaintRepository complaintRepository, IMapper mapper)
        {
            _complaintRepository = complaintRepository;
            _mapper = mapper;
        }

        public async Task<ComplaintDto?> CreateComplaintAsync(int userId, CreateComplaintDto dto)
        {
            var complaint = _mapper.Map<Complaint>(dto);
            complaint.UserId = userId;

            var createdComplaint = await _complaintRepository.AddAsync(complaint);
            return _mapper.Map<ComplaintDto>(createdComplaint);
        }

        public async Task<ComplaintDto?> GetComplaintByIdAsync(int complaintId)
        {
            var complaint = await _complaintRepository.GetByIdAsync(complaintId);
            return complaint == null ? null : _mapper.Map<ComplaintDto>(complaint);
        }

        public async Task<IEnumerable<ComplaintDto>> GetComplaintsByUserIdAsync(int userId)
        {
            var complaints = await _complaintRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<ComplaintDto>>(complaints);
        }

        public async Task<IEnumerable<ComplaintDto>> GetAllComplaintsAsync()
        {
            var complaints = await _complaintRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ComplaintDto>>(complaints);
        }

        public async Task<ComplaintDto?> UpdateComplaintAsync(int complaintId, UpdateComplaintDto dto)
        {
            var complaint = await _complaintRepository.GetByIdAsync(complaintId);
            if (complaint == null)
                return null;

            _mapper.Map(dto, complaint);
            var updatedComplaint = await _complaintRepository.UpdateAsync(complaint);
            return updatedComplaint == null ? null : _mapper.Map<ComplaintDto>(updatedComplaint);
        }

        public async Task<bool> DeleteComplaintAsync(int complaintId)
        {
            return await _complaintRepository.DeleteAsync(complaintId);
        }

        public async Task<int> GetComplaintCountAsync()
        {
            return await _complaintRepository.GetCountAsync();
        }
    }
}