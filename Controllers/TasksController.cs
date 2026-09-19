using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NagsterApi.Data;
using NagsterApi.Models;

namespace NagsterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(NagsterContext db) : ControllerBase
{
    // GET /api/tasks – nyaste först
    [HttpGet]
    public async Task<IEnumerable<NagTask>> GetAll()
    {
        return await db.Tasks.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    // GET /api/tasks/5
    [HttpGet("{id}")]
    public async Task<ActionResult<NagTask>> GetOne(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();
        return task;
    }

    // POST /api/tasks
    [HttpPost]
    public async Task<ActionResult<NagTask>> Create(NagTask task)
    {
        task.Id = 0; // id sätts av databasen, inte av klienten
        task.CreatedAt = DateTime.UtcNow;
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOne), new { id = task.Id }, task);
    }
}
