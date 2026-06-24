using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TravelHub.Service
{
    public class TempFileCleanupService : BackgroundService
    {
        private readonly ILogger<TempFileCleanupService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Run every 1 hour
        private readonly TimeSpan _fileAgeLimit = TimeSpan.FromHours(24); // Delete files older than 24 hours

        public TempFileCleanupService(ILogger<TempFileCleanupService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Temp File Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupTempFiles();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up temp files.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Temp File Cleanup Service is stopping.");
        }

        private void CleanupTempFiles()
        {
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var tempFolder = Path.Combine(webRootPath, "uploads", "temp");

            if (!Directory.Exists(tempFolder))
            {
                return;
            }

            var files = Directory.GetFiles(tempFolder);
            int deletedCount = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                // Check if file is older than the limit
                if (DateTime.UtcNow - fileInfo.CreationTimeUtc > _fileAgeLimit)
                {
                    try
                    {
                        fileInfo.Delete();
                        deletedCount++;
                        _logger.LogInformation("Deleted old temp file: {FileName}", fileInfo.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {FileName}", fileInfo.Name);
                    }
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleanup finished. Deleted {DeletedCount} old temp files.", deletedCount);
            }
        }
    }
}
