using Api.Data;
using Api.Dtos;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScriptsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ScriptExecutor _executor;
        private readonly ScriptValidationService _validator;

        public ScriptsController(AppDbContext db, ScriptExecutor executor, ScriptValidationService validator)
        {
            _db = db;
            _executor = executor;
            _validator = validator;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ScriptResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] ScriptCreateDto dto)
        {
            // Validação de formato/tamanho/tokens
            var shallow = _validator.ShallowGuards(dto.Code);
            if (shallow is not null) return BadRequest(new { error = shallow });
            
            // AST validation
            try 
            { 
                _validator.ValidateAst(dto.Code); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }          
                           
            
            // valida sintaxe e existência de process(data)
            try
            {
                _executor.Validate(dto.Code);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Script inválido: {ex.GetBaseException().Message}" });
            }

            var entity = new Script
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Code = dto.Code,
                UploadedAt = DateTime.UtcNow
            };

            _db.Scripts.Add(entity);
            await _db.SaveChangesAsync();

            var resp = new ScriptResponseDto(entity.Id, entity.Name, entity.UploadedAt);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, resp);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ScriptResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var script = await _db.Scripts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (script is null) return NotFound();

            return Ok(new ScriptResponseDto(script.Id, script.Name, script.UploadedAt));
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ScriptResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ScriptResponseDto>>> GetAll()
        {
            var list = await _db.Scripts
                .AsNoTracking()
                .Select(s => new ScriptResponseDto(s.Id, s.Name, s.UploadedAt))
                .ToListAsync();

            return Ok(list);
        }
    }
}
