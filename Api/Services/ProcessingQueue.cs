using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api.Services
{
    public class ProcessingQueue : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ScriptExecutor _executor;
        private readonly ILogger<ProcessingQueue> _logger;

        public ProcessingQueue(IServiceScopeFactory scopeFactory, ScriptExecutor executor, ILogger<ProcessingQueue> logger)
        {
            _scopeFactory = scopeFactory;
            _executor = executor; 
            _logger = logger; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var job = await db.ProcessingJobs
                        .Where(j => j.Status == JobStatus.Pending)
                        .OrderBy(j => j.CreatedAt)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (job is null)
                    {
                        await Task.Delay(2000, stoppingToken);
                        continue;
                    }

                    job.Status = JobStatus.Running;
                    await db.SaveChangesAsync(stoppingToken);

                    // carrega o Script pelo ScriptId (sem depender de navegação)
                    var script = await db.Scripts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == job.ScriptId, stoppingToken);

                    if (script is null)
                    {
                        job.ResultData = JsonSerializer.Serialize(new { error = "Script not found" });
                        job.Status = JobStatus.Failed;
                        job.FinishedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    try
                    {
                        job.ResultData = _executor.Execute(script, job.InputData);
                        job.Status = JobStatus.Completed;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha ao executar job {JobId}", job.Id);
                        job.ResultData = JsonSerializer.Serialize(new { error = ex.GetBaseException().Message });
                        job.Status = JobStatus.Failed;
                    }

                    job.FinishedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro inesperado no loop do ProcessingQueue.");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
