using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/launch")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class LaunchController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public LaunchController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetReadiness()
    {
        var projectRoot = ResolveProjectRoot();
        var artifacts = BuildArtifacts(projectRoot);
        var prepared = artifacts.Count(item => item.Prepared);
        var score = artifacts.Length == 0 ? 0 : (int)Math.Round(prepared * 100d / artifacts.Length);

        return Ok(new
        {
            success = true,
            data = new
            {
                phase = 16,
                title = "Documentation, Training & Handover",
                status = score == 100 ? "Handover package prepared" : "Documentation action required",
                preparationScore = score,
                preparedArtifacts = prepared,
                totalArtifacts = artifacts.Length,
                artifacts,
                signOff = new
                {
                    uatTemplatePrepared = System.IO.File.Exists(Path.Combine(projectRoot, "tests", "uat", "UAT-SIGNOFF-TEMPLATE.md")),
                    stakeholderApprovalRecorded = false,
                    note = "A template is not approval. Record real names, dates, results and signatures before production launch."
                },
                goLiveGates = new[]
                {
                    Gate("quality", "Phase 14 quality gates executed", "Run scripts/run-quality-gates.ps1 and attach real reports."),
                    Gate("security", "Security baseline reviewed", "Review ZAP findings and close every Critical or Major issue."),
                    Gate("backup", "Backup restore rehearsed", "Restore an RDS snapshot and S3 evidence in a non-production environment."),
                    Gate("deployment", "Phase 15 deployment verified", "Run deployment verification against the approved release candidate."),
                    Gate("training", "Role training completed", "Record attendance for Citizen, Officer, Supervisor and Admin sessions."),
                    Gate("support", "Support ownership accepted", "Assign incident, database, storage and application owners."),
                    Gate("approval", "Stakeholder go-live approval", "Capture formal approval only after all evidence is reviewed.")
                },
                commands = new[]
                {
                    "scripts/operations/go-live-validation.ps1",
                    "scripts/operations/generate-project-evidence.ps1",
                    "scripts/operations/maintenance-report.ps1",
                    "scripts/operations/create-handover-bundle.ps1"
                },
                generatedAtUtc = DateTimeOffset.UtcNow,
                environment = _environment.EnvironmentName,
                note = "This endpoint reports whether Phase 16 artifacts are present. It never claims that UAT, security, backup or stakeholder approvals passed without real evidence."
            }
        });
    }

    [HttpGet("document-catalog")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDocumentCatalog()
    {
        var projectRoot = ResolveProjectRoot();
        var artifacts = BuildArtifacts(projectRoot);

        return Ok(new
        {
            success = true,
            data = artifacts.Select(item => new
            {
                item.Key,
                item.Name,
                item.Path,
                item.Audience,
                item.Prepared
            })
        });
    }

    private string ResolveProjectRoot()
    {
        var directory = new DirectoryInfo(_environment.ContentRootPath);
        return directory.Parent?.Parent?.FullName ?? _environment.ContentRootPath;
    }

    private static LaunchArtifact[] BuildArtifacts(string projectRoot) =>
    [
        Artifact(projectRoot, "srs", "Software Requirements Specification", "docs/requirements/SRS.md", "Evaluators, product owners and developers"),
        Artifact(projectRoot, "hld", "High-Level Design", "docs/architecture/HLD.md", "Architects and technical evaluators"),
        Artifact(projectRoot, "lld", "Low-Level Design", "docs/architecture/LLD.md", "Developers and maintainers"),
        Artifact(projectRoot, "database", "Database Design", "docs/database/DATABASE-DESIGN.md", "Backend and database teams"),
        Artifact(projectRoot, "api", "API Reference", "docs/api/API-REFERENCE.md", "Frontend, backend and integration teams"),
        Artifact(projectRoot, "citizen-training", "Citizen Training Guide", "docs/training/CITIZEN-TRAINING.md", "Citizens and help-desk staff"),
        Artifact(projectRoot, "officer-training", "Officer Training Guide", "docs/training/OFFICER-TRAINING.md", "Field officers"),
        Artifact(projectRoot, "supervisor-training", "Supervisor Training Guide", "docs/training/SUPERVISOR-TRAINING.md", "Department supervisors"),
        Artifact(projectRoot, "admin-training", "Administrator Training Guide", "docs/training/ADMIN-TRAINING.md", "Administrators and SuperAdmins"),
        Artifact(projectRoot, "maintenance", "Maintenance Plan", "docs/operations/MAINTENANCE-PLAN.md", "Operations and support teams"),
        Artifact(projectRoot, "incident", "Incident Response Runbook", "docs/operations/INCIDENT-RESPONSE.md", "Operations and security teams"),
        Artifact(projectRoot, "handover", "Project Handover", "docs/handover/PROJECT-HANDOVER.md", "Project owner and maintenance team"),
        Artifact(projectRoot, "report", "Final Project Report", "docs/FINAL-PROJECT-REPORT.md", "Academic evaluators and stakeholders"),
        Artifact(projectRoot, "roadmap", "Future Product Roadmap", "docs/FUTURE-ROADMAP.md", "Product owner and future teams")
    ];

    private static LaunchArtifact Artifact(string root, string key, string name, string path, string audience)
    {
        var repositoryPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
        var packagedPath = path.StartsWith("docs/", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(AppContext.BaseDirectory, "project-docs", path[5..].Replace('/', Path.DirectorySeparatorChar))
            : string.Empty;

        return new(key, name, path, audience,
            System.IO.File.Exists(repositoryPath) || (!string.IsNullOrEmpty(packagedPath) && System.IO.File.Exists(packagedPath)));
    }

    private static GoLiveGate Gate(string key, string name, string evidence) =>
        new(key, name, "Evidence required", evidence);

    private sealed record LaunchArtifact(string Key, string Name, string Path, string Audience, bool Prepared);
    private sealed record GoLiveGate(string Key, string Name, string Status, string Evidence);
}
