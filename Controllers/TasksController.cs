using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NagsterApi.Data;
using NagsterApi.Models;

namespace NagsterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(NagsterContext db, IWebHostEnvironment env) : ControllerBase
{
    private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

    // Vitlista: bara de ljudformat apparna faktiskt spelar in i.
    // webm: Chrome och Edge, ogg: Firefox, m4a: Safari och mobilappen.
    private static readonly string[] AllowedExtensions = [".webm", ".ogg", ".m4a"];

    // GET /api/tasks - nyaste först
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

    // PUT /api/tasks/5 - används för snooze och för att markera klart
    [HttpPut("{id}")]
    public async Task<ActionResult<NagTask>> Update(int id, NagTask updated)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        task.Title = updated.Title;
        task.Status = updated.Status;
        task.SnoozeCount = updated.SnoozeCount;
        task.DurationSeconds = updated.DurationSeconds;
        await db.SaveChangesAsync();
        return task;
    }

    // POST /api/tasks/5/file - multipart/form-data med fältnamnet "file"
    [HttpPost("{id}/file")]
    [RequestSizeLimit(MaxFileBytes)]
    public async Task<ActionResult<NagTask>> UploadFile(int id, IFormFile file)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        if (file.Length == 0) return BadRequest("Filen är tom.");
        if (file.Length > MaxFileBytes) return BadRequest("Filen är för stor (max 10 MB).");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension)) return BadRequest("Endast ljudfiler tillåts.");

        // Ändelsen och den angivna typen kommer från klienten och går att fejka.
        // Filens innehåll går inte att ljuga om, så det kontrolleras också.
        if (!file.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
            || !await IsAudioFileAsync(file, extension))
            return BadRequest("Filen är inte en giltig ljudfil.");

        var uploadsDir = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        // Slumpat filnamn så två uppladdningar aldrig skriver över varandra,
        // och så att ett filnamn från användaren aldrig hamnar på disken
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var target = System.IO.File.Create(Path.Combine(uploadsDir, fileName)))
        {
            await file.CopyToAsync(target);
        }

        // Bara sökvägen sparas i databasen - filen ligger på disk
        task.FilePath = $"/uploads/{fileName}";
        await db.SaveChangesAsync();
        return task;
    }

    // Varje filformat börjar med en egen stämpel, så kallade "magic bytes".
    // Här läses filens första bytes och jämförs med stämpeln ändelsen utlovar.
    private static async Task<bool> IsAudioFileAsync(IFormFile file, string extension)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        if (read < header.Length) return false;

        return extension switch
        {
            ".webm" => header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            ".ogg" => Encoding.ASCII.GetString(header, 0, 4) == "OggS",
            ".m4a" => Encoding.ASCII.GetString(header, 4, 4) == "ftyp", // stämpeln ligger en bit in
            _ => false,
        };
    }
}
