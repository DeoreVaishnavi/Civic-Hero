using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Enums;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Infrastructure.Repositories;

public class ComplaintRepository : IComplaintRepository
{
    private readonly CivicHeroDbContext _context;

    public ComplaintRepository(CivicHeroDbContext context)
    {
        _context = context;
    }

    public async Task<Complaint?> GetByIdAsync(int id)
    {
        return await _context.Complaints
            .Include(x => x.Timeline)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Complaint>> GetAllAsync(
        ComplaintQueryParameters query)
    {
        IQueryable<Complaint> complaints =
            _context.Complaints.AsNoTracking();

        if (query.Status.HasValue)
            complaints = complaints.Where(
                x => x.Status == query.Status.Value);

        if (query.Priority.HasValue)
            complaints = complaints.Where(
                x => x.Priority == query.Priority.Value);

        if (query.WardId.HasValue)
            complaints = complaints.Where(
                x => x.WardId == query.WardId.Value);

        if (query.DepartmentId.HasValue)
            complaints = complaints.Where(
                x => x.DepartmentId == query.DepartmentId.Value);

        if (query.FromDate.HasValue)
            complaints = complaints.Where(
                x => x.CreatedAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            complaints = complaints.Where(
                x => x.CreatedAt <= query.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();

            complaints = complaints.Where(x =>
                x.Title.Contains(search) ||
                x.Description.Contains(search) ||
                x.Address.Contains(search));
        }

        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);

        return await complaints
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Complaint> CreateAsync(
        Complaint complaint)
    {
        await _context.Complaints.AddAsync(complaint);

        await _context.SaveChangesAsync();

        return complaint;
    }

    public async Task UpdateAsync(Complaint complaint)
    {
        _context.Complaints.Update(complaint);

        await _context.SaveChangesAsync();
    }

    public async Task<List<Complaint>> GetByWardAsync(
        int wardId)
    {
        return await _context.Complaints
            .AsNoTracking()
            .Where(x => x.WardId == wardId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<DashboardCounterDto>
        GetDashboardStatsAsync()
    {
        return new DashboardCounterDto
        {
            TotalCount =
                await _context.Complaints.CountAsync(),

            OpenCount =
                await _context.Complaints.CountAsync(x =>
                    x.Status != ComplaintStatus.Resolved &&
                    x.Status != ComplaintStatus.Closed &&
                    x.Status != ComplaintStatus.Rejected &&
                    x.Status != ComplaintStatus.Archived),

            ResolvedCount =
                await _context.Complaints.CountAsync(x =>
                    x.Status == ComplaintStatus.Resolved ||
                    x.Status == ComplaintStatus.Closed),

            InProgressCount =
                await _context.Complaints.CountAsync(x =>
                    x.Status == ComplaintStatus.InProgress),

            RejectedCount =
                await _context.Complaints.CountAsync(x =>
                    x.Status == ComplaintStatus.Rejected)
        };
    }

    public async Task AddTimelineAsync(
        ComplaintTimeline timeline)
    {
        await _context.ComplaintTimelines.AddAsync(timeline);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Complaint>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        var complaints = await _context.Complaints
            .AsNoTracking()
            .ToListAsync();

        return complaints.Where(c =>
            CalculateDistance(
                latitude,
                longitude,
                c.Latitude,
                c.Longitude) <= radiusKm)
            .ToList();
    }

    private static double CalculateDistance(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadius = 6371;

        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return earthRadius * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}