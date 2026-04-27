namespace DanmakuDownloader.Models.Database;

public interface IEpisode
{
    int     SubjectId  { get; }
    decimal EpisodeNum { get; }
}
