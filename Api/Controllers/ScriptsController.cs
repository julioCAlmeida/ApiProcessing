using ApiPreProcessamento.Data;
using ApiPreProcessamento.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiPreProcessamento.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScriptsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ScriptsController(AppDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Upload([FromBody] Script script)
        {
            _db.Scripts.Add(script);
            await _db.SaveChangesAsync();
            return Ok(script);
        }

        [HttpGet]
        public IActionResult GetAll() => Ok(_db.Scripts.ToList());
    }
}
