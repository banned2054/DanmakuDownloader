namespace DanmakuDownloader.Web.Models.Job;

public class DanmakuJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // 业务关联信息
    public int    SubjectId  { get; set; }
    public int    EpisodeNum { get; set; }
    public string TargetPath { get; set; } = string.Empty;

    // 状态机核心字段
    public JobStatus Status      { get; set; } = JobStatus.Pending;
    public int       RetryCount  { get; set; } = 0;
    public DateTime  NextRunTime { get; set; } = DateTime.UtcNow;

    public string? LastError { get; set; }

    // 并发控制：改用 Guid，兼容性更好
    // 每次更新数据时，手动或通过拦截器更新此字段
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}