using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NagsterApi.Data;
using NagsterApi.Models;

namespace NagsterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(NagsterContext db, IWebHostEnvironment env) : ControllerBase
{
    // Bara ljud och bild – vi tar inte emot vad som helst
    private static readonly string[] AllowedExtensions =
        [".webm", ".m4a", ".mp3", ".wav", ".ogg", ".png", ".jpg", ".jpeg", ".gif"];

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

    // PUT /api/tasks/5 – används för snooze och för att markera klart
    [HttpPut("{id}")]
    public async Task<ActionResult<NagTask>> Update(int id, NagTask updated)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        task.Title = updated.Title;
        task.Status = updated.Status;
        task.SnoozeCount = updated.SnoozeCount;
        task.DurationMinutes = updated.DurationMinutes;
        await db.SaveChangesAsync();
        return task;
    }

    // POST /api/tasks/5/file – multipart/form-data med fältnamnet "file"
    [HttpPost("{id}/file")]
    public async Task<ActionResult<NagTask>> UploadFile(int id, IFormFile file)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        if (file.Length == 0) return BadRequest("Filen är tom.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Endast ljud- och bildfiler tillåts.");

        var uploadsDir = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        // Slumpat filnamn så två uppladdningar aldrig skriver över varandra
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(uploadsDir, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        // Bara sökvägen sparas i databasen – filen ligger på disk
        task.FilePath = $"/uploads/{fileName}";
        await db.SaveChangesAsync();
        return task;
    }
}
