using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/final-release")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class FinalReleaseController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public FinalReleaseController(IWebHostEnvironment environment) => _environment = environment;

    [HttpGet("readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetReadiness()
    {
        var projectRoot = ResolveProjectRoot();
        var evidencePath = Path.Combine(projectRoot, "artifacts", "phase18", "release-evidence.json");
        var evidence = ReadEvidence(evidencePath);
        var gates = evidence?.Gates ?? Array.Empty<ReleaseGateEvidence>();
        var required = gates.Where(item => item.Required).ToArray();
        var passed = required.Count(item => item.Status.Equals("Passed", StringComparison.OrdinalIgnoreCase));
        var failed = required.Count(item => item.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
        var skipped = required.Count(item => item.Status.Equals("Skipped", StringComparison.OrdinalIgnoreCase));
        var approved = evidence is not null && evidence.Approved && required.Length > 0 && passed == required.Length;

        return Ok(new
        {
            success = true,
            data = new
            {
                phase = 18,
                title = "Testing, Docker, CI/CD & Production Release",
                status = approved ? "Release approved by evidence" : evidence is null ? "Evidence not generated" : "Release blocked",
                approved,
                evidencePresent = evidence is not null,
                releaseVersion = evidence?.ReleaseVersion ?? "not assigned",
                gitCommit = evidence?.GitCommit ?? "not recorded",
                generatedAtUtc = evidence?.GeneratedAtUtc,
                summary = new
                {
                    totalRequired = required.Length,
                    passed,
                    failed,
                    skipped,
                    totalGates = gates.Length
                },
                gates,
                requiredEvidence = new[]
                {
                    "Backend and frontend build/test reports",
                    "Clean-database migration result",
                    "Critical complaint E2E result",
                    "Newman API regression result",
                    "k6 performance result",
                    "OWASP ZAP security result",
                    "Docker deployment verification",
                    "Monitoring verification",
                    "Backup restore rehearsal",
                    "Signed UAT evidence",
                    "Clean Git commit and approved release tag"
                },
                commands = new[]
                {
                    "scripts/release/run-phase18-release-gates.ps1",
                    "scripts/release/create-release-candidate.ps1"
                },
                evidenceFile = "artifacts/phase18/release-evidence.json",
                note = "Phase 18 is complete only when every required gate has real Passed evidence. Missing or skipped evidence blocks approval."
            }
        });
    }

    private string ResolveProjectRoot()
    {
        var directory = new DirectoryInfo(_environment.ContentRootPath);
        return directory.Parent?.Parent?.FullName ?? _environment.ContentRootPath;
    }

    private static ReleaseEvidence? ReadEvidence(string path)
    {
        if (!System.IO.File.Exists(path)) return null;
        try
        {
            var json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<ReleaseEvidence>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    public sealed class ReleaseEvidence
    {
        public string ReleaseVersion { get; set; } = string.Empty;
        public string GitCommit { get; set; } = string.Empty;
        public DateTimeOffset? GeneratedAtUtc { get; set; }
        public bool Approved { get; set; }
        public ReleaseGateEvidence[] Gates { get; set; } = Array.Empty<ReleaseGateEvidence>();
    }

    public sealed class ReleaseGateEvidence
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "NotRun";
        public bool Required { get; set; } = true;
        public string Detail { get; set; } = string.Empty;
        public string? EvidencePath { get; set; }
        public double DurationSeconds { get; set; }
    }
}
