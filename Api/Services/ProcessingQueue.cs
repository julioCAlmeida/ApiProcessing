using ApiPreProcessamento.Data;
using ApiPreProcessamento.Models;

namespace ApiPreProcessamento.Services
{
    public class ProcessingQueue : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ScriptExecutor _executor;

        public ProcessingQueue(IServiceScopeFactory scopeFactory, ScriptExecutor executor)
        {
            _scopeFactory = scopeFactory;
            _executor = executor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var job = db.ProcessingJobs
                            .Where(j => j.Status == JobStatus.Pending)
                            .OrderBy(j => j.CreatedAt)
                            .FirstOrDefault();
                if (job != null)
                {
                    job.Status = JobStatus.Running;
                    await db.SaveChangesAsync();

                    try
                    {
                        job.ResultData = _executor.Execute(job.Script, job.InputData);
                        job.Status = JobStatus.Completed;
                        job.FinishedAt = DateTime.UtcNow;
                    }
                    catch
                    {
                        job.Status = JobStatus.Failed;
                    }

                    await db.SaveChangesAsync();
                }

                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}

