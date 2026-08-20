using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Services;

public static class AuditLogQueryFilter
{
    public static IQueryable<AuditLog> Apply(IQueryable<AuditLog> source, AuditLogQuery query)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x =>
                (x.UserEmail != null && x.UserEmail.Contains(search)) ||
                x.Action.Contains(search) ||
                x.EntityName.Contains(search) ||
                (x.EntityId != null && x.EntityId.Contains(search)) ||
                (x.CorrelationId != null && x.CorrelationId.Contains(search)) ||
                (x.IpAddress != null && x.IpAddress.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            var ipAddress = query.IpAddress.Trim();
            source = source.Where(x => x.IpAddress != null && x.IpAddress.Contains(ipAddress));
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
            source = source.Where(x => x.Action == query.Action.Trim());
        if (!string.IsNullOrWhiteSpace(query.EntityName))
            source = source.Where(x => x.EntityName == query.EntityName.Trim());
        if (!string.IsNullOrWhiteSpace(query.UserRole))
            source = source.Where(x => x.UserRole == query.UserRole.Trim());
        if (query.Success.HasValue)
            source = source.Where(x => x.Success == query.Success.Value);
        if (query.From.HasValue)
            source = source.Where(x => x.CreatedAt >= query.From.Value);
        if (query.To.HasValue)
            source = source.Where(x => x.CreatedAt <= query.To.Value);

        return source;
    }
}
