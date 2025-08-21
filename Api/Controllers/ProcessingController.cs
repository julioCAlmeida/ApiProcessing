using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProcessingController(AppDbContext db) => _db = db;

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateJob([FromBody] ProcessingCreateDto dto)
        {
            var script = await _db.Scripts.FindAsync(dto.ScriptId);
            if (script == null) return NotFound("Script not found");

            var job = new ProcessingJob
            {
                ScriptId = dto.ScriptId,
                InputData = JsonSerializer.Serialize(dto.Data),
                Status = JobStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.ProcessingJobs.Add(job);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = job.Id }, job.Id);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProcessingJobDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid id)
        {
            var job = await _db.ProcessingJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
            if (job is null) return NotFound();

            JsonElement? result = null;
            if (!string.IsNullOrWhiteSpace(job.ResultData))
            {
                using var doc = JsonDocument.Parse(job.ResultData);
                result = doc.RootElement.Clone();
            }

            var dto = new ProcessingJobDto(job.Id, job.ScriptId, job.Status, job.CreatedAt, job.FinishedAt, result);
            return Ok(dto);
        }
    }
}
