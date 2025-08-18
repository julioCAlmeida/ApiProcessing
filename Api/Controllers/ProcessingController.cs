using ApiPreProcessamento.Data;
using ApiPreProcessamento.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiPreProcessamento.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProcessingController(AppDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> CreateJob(Guid scriptId, [FromBody] object inputData)
        {
            var script = await _db.Scripts.FindAsync(scriptId);
            if (script == null) return NotFound("Script not found");

            var job = new ProcessingJob
            {
                ScriptId = scriptId,
                Script = script,
                InputData = System.Text.Json.JsonSerializer.Serialize(inputData)
            };

            _db.ProcessingJobs.Add(job);
            await _db.SaveChangesAsync();

            return Ok(job.Id);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJob(Guid id)
        {
            var job = await _db.ProcessingJobs.FindAsync(id);
            return job != null ? Ok(job) : NotFound();
        }
    }
}
