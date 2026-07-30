using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.Data;

namespace CivicHero.Backend.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for processing fraud analysis complaints.
/// Periodically checks for new complaints and runs fraud analysis on them.
/// </summary>
public class FraudAnalysisWorker : BackgroundService
{
    private readonly ILogger<FraudAnalysisWorker> _logger;
    private readonly IFraudAnalysisService _fraudAnalysisService;
    private readonly CivicHeroDbContext _context;
    private Timer _timer;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30); // Run every 30 minutes

    public FraudAnalysisWorker(
        ILogger<FraudAnalysisWorker> logger,
        IFraudAnalysisService fraudAnalysisService,
        CivicHeroDbContext context)
    {
        _logger = logger;
        _fraudAnalysisService = fraudAnalysisService;
        _context = context;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FraudAnalysisWorker starting.");

        // Create a timer that starts immediately and repeats every interval
        _timer = new Timer(DoWork, null, TimeSpan.Zero, _interval);

        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        _logger.LogInformation("FraudAnalysisWorker performing background work at: {time}", DateTimeOffset.Now);

        try
        {
            ProcessPendingComplaints();

            // Optional: Log statistics
            LogStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in FraudAnalysisWorker.");
        }
    }

    /// <summary>
    /// Processes complaints that don't yet have fraud analysis.
    /// </summary>
    private void ProcessPendingComplaints()
    {
        // Find complaints that don't have fraud analysis yet
        var complaintsWithoutAnalysis = _context.Complaints
            .Where(c => !_context.AiFraudAnalyses.Any(a => a.ComplaintId == c.Id))
            .Take(50) // Process in batches to avoid overwhelming the system
            .ToList();

        _logger.LogInformation("Found {count} complaints without fraud analysis", complaintsWithoutAnalysis.Count);

        foreach (var complaint in complaintsWithoutAnalysis)
        {
            try
            {
                // Perform fraud analysis on the complaint
                // Note: In a real implementation, we would need the complaint text, images, etc.
                // For this background worker, we'll use placeholder data
                // In practice, you'd want to fetch the actual complaint details

                var analysis = _fraudAnalysisService.AnalyzeComplaintForFraud(
                    complaint.Id,
                    $"Complaint: {complaint.Title} - {complaint.Description}",
                    null, // No image in background processing for now
                    complaint.Latitude,
                    complaint.Longitude,
                    complaint.CreatedAt).Result;

                _logger.LogInformation(
                    "Completed fraud analysis for complaint {complaintId}: Score={score}, RiskLevel={risk}",
                    complaint.Id, analysis.FraudScore, analysis.RiskLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process fraud analysis for complaint {complaintId}", complaint.Id);
            }
        }
    }

    /// <summary>
    /// Logs statistics about the fraud analysis system.
    /// </summary>
    private void LogStatistics()
    {
        try
        {
            var totalComplaints = _context.Complaints.Count();
            var analyzedComplaints = _context.AiFraudAnalyses.Count();
            var pendingAnalysis = totalComplaints - analyzedComplaints;

            var averageScore = _fraudAnalysisService.CalculateAverageFraudScore().Result;

            _logger.LogInformation(
                "Fraud Analysis Statistics: Total Complaints={total}, Analyzed={analyzed}, Pending={pending}, Average Score={avgScore:F2}",
                totalComplaints, analyzedComplaints, pendingAnalysis, averageScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log fraud analysis statistics");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FraudAnalysisWorker stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // This method is called when the background service starts.
        // We've already set up the timer in StartAsync, so we can just return a completed task.
        // Alternatively, we could use the built-in loop from BackgroundService by overriding ExecuteAsync.
        // But since we are using a timer, we leave this empty and rely on Start/Stop.
        return Task.CompletedTask;
    }
}