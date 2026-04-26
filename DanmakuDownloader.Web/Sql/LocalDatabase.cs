using DanmakuDownloader.Web.Models.Job;
using Microsoft.EntityFrameworkCore;

namespace DanmakuDownloader.Web.Sql;

public class LocalDatabase : DbContext
{
    public DbSet<DanmakuJob> DanmakuJobs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=job.db");
    }

    public override int SaveChanges()
    {
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries()
                                   .Where(e => e is { Entity: DanmakuJob, State: EntityState.Modified });

        foreach (var entry in entries)
        {
            ((DanmakuJob)entry.Entity).UpdatedAt = now;
        }

        return base.SaveChanges();
    }
}