using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicHero.Backend.Controllers;

[ApiController]
[Route("api/v1/quality")]
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class QualityController : ControllerBase
{
    [HttpGet("readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetReadiness()
    {
        var categories = new[]
        {
            new { key = "backend-unit", name = "Backend unit tests", tool = "xUnit", status = "Configured", target = "Domain 90% / API 80%" },
            new { key = "backend-api", name = "Backend API smoke tests", tool = "WebApplicationFactory", status = "Configured", target = "Critical endpoints" },
            new { key = "frontend-unit", name = "Frontend unit tests", tool = "Vitest + React Testing Library", status = "Configured", target = "Frontend 75%" },
            new { key = "api-contract", name = "API contract tests", tool = "Postman + Newman", status = "Configured", target = "Positive and negative paths" },
            new { key = "browser-e2e", name = "Browser smoke tests", tool = "Playwright", status = "Configured", target = "Citizen and Admin entry flows" },
            new { key = "performance", name = "Performance baseline", tool = "k6", status = "Configured", target = "Health p95 < 500 ms; errors < 1%" },
            new { key = "security", name = "Security baseline", tool = "OWASP ZAP", status = "Configured", target = "No high-risk findings" },
            new { key = "uat", name = "User acceptance testing", tool = "UAT checklist", status = "Ready", target = ">= 95% pass; zero critical/major defects" }
        };

        return Ok(new
        {
            success = true,
            data = new
            {
                phase = 14,
                title = "Testing, Quality Assurance & UAT",
                readiness = "Quality gates configured",
                note = "Run scripts/run-quality-gates.ps1 to generate actual reports. Configured does not mean passed.",
                categories,
                coverageTargets = new { domain = 90, application = 85, api = 80, frontend = 75, criticalFlows = 100 },
                releaseGates = new[]
                {
                    "Backend and frontend builds succeed",
                    "Automated unit and smoke tests pass",
                    "Critical E2E smoke tests pass",
                    "No critical or major UAT defects remain open",
                    "Performance and security baselines meet documented thresholds"
                },
                generatedAtUtc = DateTimeOffset.UtcNow
            }
        });
    }
}
