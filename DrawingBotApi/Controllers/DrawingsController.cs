using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DrawingBotApi.Data;
using DrawingBotApi.Models;

namespace DrawingBotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DrawingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DrawingsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("save")]
        public async Task<ActionResult<SaveDrawingResponse>> SaveDrawing([FromBody] SaveDrawingRequest request)
        {
            if (request == null || request.Commands == null || request.Commands.Count == 0)
            {
                return BadRequest("Drawing commands are required.");
            }

            var drawing = new Drawing
            {
                UserId = string.IsNullOrWhiteSpace(request.UserId) ? "default-user" : request.UserId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? "Untitled Drawing" : request.Title,
                CommandsJson = JsonSerializer.Serialize(request.Commands),
                CreatedAt = DateTime.UtcNow
            };

            _context.Drawings.Add(drawing);
            await _context.SaveChangesAsync();

            return Ok(new SaveDrawingResponse
            {
                Id = drawing.Id,
                Message = "Drawing saved to server."
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LoadDrawingResponse>> GetDrawing(int id)
        {
            var drawing = await _context.Drawings.FirstOrDefaultAsync(d => d.Id == id);

            if (drawing == null)
            {
                return NotFound("Drawing not found.");
            }

            var commands = JsonSerializer.Deserialize<List<DrawingCommand>>(
                drawing.CommandsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new List<DrawingCommand>();

            return Ok(new LoadDrawingResponse
            {
                Id = drawing.Id,
                UserId = drawing.UserId,
                Title = drawing.Title,
                Commands = commands
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<LoadDrawingResponse>>> GetAllDrawings()
        {
            var drawings = await _context.Drawings
                .OrderBy(d => d.Id)
                .ToListAsync();

            var result = drawings.Select(d => new LoadDrawingResponse
            {
                Id = d.Id,
                UserId = d.UserId,
                Title = d.Title,
                Commands = JsonSerializer.Deserialize<List<DrawingCommand>>(
                    d.CommandsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<DrawingCommand>()
            }).ToList();

            return Ok(result);
        }
    }
}