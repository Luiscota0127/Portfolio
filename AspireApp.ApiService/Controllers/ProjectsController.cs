using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspireApp.ApiService.Data;
using AspireApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace AspireApp.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProjectsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var projects = await _db.Projects.Include(p => p.Tasks).ToListAsync();
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var project = await _db.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
    {
        var project = new Project { Name = dto.Name, Description = dto.Description, OwnerId = User?.Identity?.Name };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProjectUpdateDto dto)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return NotFound();
        project.Name = dto.Name;
        project.Description = dto.Description;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return NotFound();
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record ProjectCreateDto(string Name, string? Description);
public record ProjectUpdateDto(string Name, string? Description);
