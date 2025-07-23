﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DanmakuDownloader.Models.Database;

[Table("episodeList")]
public class Episode
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("subjectId")]
    public int SubjectId { get; set; }

    [Column("episode")]
    public decimal EpisodeNum { get; set; }
}