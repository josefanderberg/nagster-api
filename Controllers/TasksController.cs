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

    // Vitlista: bara det appen faktiskt använder. Ljud för tjatet, bild som komplement.
    private static readonly string[] AllowedAudio = [".webm", ".m4a", ".mp3", ".wav"];
    private static readonly string[] AllowedImages = [".png", ".jpg", ".jpeg"];

    // Filers första bytes ("magic bytes"). Filändelsen går att ljuga om,
    // men innehållet börjar alltid med den här stämpeln.
    private static readonly Dictionary<string, byte[][]> Signatures = new()
    {
        [".webm"] = [[0x1A, 0x45, 0xDF, 0xA3]],
        [".mp3"] = [[0x49, 0x44, 0x33], [0xFF, 0xFB], [0xFF, 0xF3], [0xFF, 0xF2]],
        [".wav"] = [[0x52, 0x49, 0x46, 0x46]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
    };

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
        task.DurationSeconds = updated.DurationSeconds;
        await db.SaveChangesAsync();
        return task;
    }

    // POST /api/tasks/5/file – multipart/form-data med fältnamnet "file"
    [HttpPost("{id}/file")]
    [RequestSizeLimit(MaxFileBytes)]
    public async Task<ActionResult<NagTask>> UploadFile(int id, IFormFile file)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return NotFound();

        if (file.Length == 0) return BadRequest("Filen är tom.");
        if (file.Length > MaxFileBytes) return BadRequest("Filen är för stor (max 10 MB).");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isAudio = AllowedAudio.Contains(extension);
        var isImage = AllowedImages.Contains(extension);
        if (!isAudio && !isImage) return BadRequest("Endast ljud- och bildfiler tillåts.");

        // Tre kontroller som måste vara överens: ändelse, angiven typ och innehåll
        var declaredType = file.ContentType ?? "";
        var expectedPrefix = isAudio ? "audio/" : "image/";
        if (!declaredType.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Filtypen stämmer inte med filändelsen.");

        if (!await HasExpectedSignatureAsync(file, extension))
            return BadRequest("Filens innehåll stämmer inte med filändelsen.");

        var uploadsDir = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        // Slumpat filnamn så två uppladdningar aldrig skriver över varandra,
        // och så att ett filnamn från användaren aldrig hamnar på disken
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var target = System.IO.File.Create(Path.Combine(uploadsDir, fileName)))
        {
            await file.CopyToAsync(target);
        }

        // Bara sökvägen sparas i databasen – filen ligger på disk
        task.FilePath = $"/uploads/{fileName}";
        await db.SaveChangesAsync();
        return task;
    }

    // Läser filens början och jämför med den stämpel filändelsen utlovar
    private static async Task<bool> HasExpectedSignatureAsync(IFormFile file, string extension)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);

        // m4a har sin stämpel ("ftyp") en bit in i filen
        if (extension == ".m4a")
            return read >= 8 && Encoding.ASCII.GetString(header, 4, 4) == "ftyp";

        return Signatures.TryGetValue(extension, out var candidates)
            && candidates.Any(signature =>
                read >= signature.Length && header.Take(signature.Length).SequenceEqual(signature));
    }
}
