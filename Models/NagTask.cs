using System.ComponentModel.DataAnnotations;

namespace NagsterApi.Models;

public class NagTask
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    // "pending" eller "done"
    public string Status { get; set; } = "pending";

    public int SnoozeCount { get; set; } = 0;

    public int DurationSeconds { get; set; } = 1500;

    // Sökväg till den inspelade ljudfilen, t.ex. /uploads/abc123.webm
    public string? FilePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
