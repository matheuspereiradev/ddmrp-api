namespace Service.API.BackgroundServices
{
    // Periodically deletes generated export CSVs older than Exports:RetentionHours. No DB tracking
    // of generated files — the directory itself, via each file's last-write time, is the source of
    // truth, same "local drop folder" treatment as Service.API/Imports.
    public class ExportCleanupBackgroundService : BackgroundService
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);

        private readonly string _directory;
        private readonly TimeSpan _retention;
        private readonly ILogger<ExportCleanupBackgroundService> _logger;

        public ExportCleanupBackgroundService(IConfiguration configuration, ILogger<ExportCleanupBackgroundService> logger)
        {
            var configuredDirectory = configuration["Exports:Directory"] ?? "Exports";
            _directory = Path.IsPathRooted(configuredDirectory) ? configuredDirectory : Path.Combine(Directory.GetCurrentDirectory(), configuredDirectory);

            var retentionHours = int.TryParse(configuration["Exports:RetentionHours"], out var hours) ? hours : 24;
            _retention = TimeSpan.FromHours(retentionHours);

            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(SweepInterval);
            do
            {
                CleanupExpiredFiles();
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private void CleanupExpiredFiles()
        {
            if (!Directory.Exists(_directory))
                return;

            foreach (var file in Directory.EnumerateFiles(_directory, "*.csv"))
            {
                try
                {
                    if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > _retention)
                        File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete expired export file '{File}'.", file);
                }
            }
        }
    }
}
