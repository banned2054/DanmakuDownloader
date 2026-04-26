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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DanmakuJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion)
                  .IsConcurrencyToken();
        });
    }
}