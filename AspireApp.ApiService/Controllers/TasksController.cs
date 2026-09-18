using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AspireApp.ApiService.Data;
using AspireApp.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace AspireApp.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TasksController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var tasks = await _db.Tasks.ToListAsync();
        return Ok(tasks);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskCreateDto dto)
    {
        var project = await _db.Projects.FindAsync(dto.ProjectId);
        if (project == null) return BadRequest("Project not found");

        var task = new TaskItem { Title = dto.Title, Description = dto.Description, ProjectId = dto.ProjectId, AssignedToId = dto.AssignedToId };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaskUpdateDto dto)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.DueDate = dto.DueDate;
        task.AssignedToId = dto.AssignedToId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record TaskCreateDto(Guid ProjectId, string Title, string? Description, string? AssignedToId);
public record TaskUpdateDto(string Title, string? Description, TaskStatus Status, DateTime? DueDate, string? AssignedToId);
