using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using CivicHero.Backend.Core.Interfaces;
using CivicHero.Backend.Infrastructure.AI;

namespace CivicHero.Backend.BackgroundServices
{
    public class FraudAnalysisWorker : BackgroundService
    {
        private readonly ILogger<FraudAnalysisWorker> _logger;
        private readonly IFraudAnalysisService _fraudAnalysisService;
        private Timer _timer;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Run every hour

        public FraudAnalysisWorker(ILogger<FraudAnalysisWorker> logger, IFraudAnalysisService fraudAnalysisService)
        {
            _logger = logger;
            _fraudAnalysisService = fraudAnalysisService;
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
                // Example: Calculate and log the average fraud score
                var averageScore = _fraudAnalysisService.CalculateAverageFraudScore().Result;
                _logger.LogInformation("Average fraud score: {score}", averageScore);

                // Additional background work could go here, such as:
                // - Re-analyzing old complaints
                // - Cleaning up old audit logs
                // - Generating reports
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in FraudAnalysisWorker.");
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
}