using System.Globalization;
using System.Text.Json;
using CivicHero.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/public/settings")]
[AllowAnonymous]
public sealed class PublicSettingsController : ControllerBase
{
    private readonly CivicDbContext _db;

    public PublicSettingsController(CivicDbContext db) => _db = db;

    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var settings = await _db.SystemSettings.AsNoTracking()
            .Where(setting => setting.IsPublic && !setting.IsSensitive)
            .OrderBy(setting => setting.Group)
            .ThenBy(setting => setting.Key)
            .Select(setting => new
            {
                setting.Key,
                setting.Value,
                setting.ValueType,
                setting.Description,
                setting.Group,
                setting.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var items = settings.Select(setting => new
        {
            key = setting.Key,
            value = ParseValue(setting.Value, setting.ValueType),
            valueType = setting.ValueType,
            description = setting.Description,
            group = setting.Group,
            updatedAt = setting.UpdatedAt
        }).ToList();

        var grouped = items
            .GroupBy(item => item.group)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(item => item.key, item => item.value),
                StringComparer.OrdinalIgnoreCase);

        return Ok(new
        {
            success = true,
            message = "Public application settings loaded.",
            data = new
            {
                items,
                grouped,
                generatedAt = DateTimeOffset.UtcNow
            }
        });
    }

    private static object? ParseValue(string value, string valueType)
    {
        var type = valueType?.Trim().ToLowerInvariant() ?? "string";
        return type switch
        {
            "boolean" or "bool" when bool.TryParse(value, out var booleanValue) => booleanValue,
            "integer" or "int" when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue) => integerValue,
            "decimal" or "number" when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue) => decimalValue,
            "json" => ParseJson(value),
            _ => value
        };
    }

    private static object ParseJson(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return value;
        }
    }
}
