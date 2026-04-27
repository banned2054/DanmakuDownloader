using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DanmakuDownloader.Models.Job;

[Index(nameof(Status), nameof(NextRunTime))]
public class DanmakuJob
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("subjectId")]
    public int SubjectId { get; set; }

    [Column("episode")]
    public int Episode { get; set; }

    [Column("targetPath")]
    public string TargetPath { get; set; } = string.Empty;

    [Column("status")]
    public JobStatus Status { get; set; } = JobStatus.Pending;

    [Column("retryCount")]
    public int RetryCount { get; set; } = 0;

    [Column("nextRunTime")]
    public DateTime NextRunTime { get; set; } = DateTime.UtcNow;

    [Column("lastError")]
    public string? LastError { get; set; }

    [ConcurrencyCheck]
    [Column("rowVersion")]
    public Guid RowVersion { get; set; } = Guid.NewGuid();

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
