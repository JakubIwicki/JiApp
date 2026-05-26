// ReSharper disable PropertyCanBeMadeInitOnly.Global

using System.ComponentModel.DataAnnotations;

namespace JiApp.Common.Models;

public class YoutubeDownloadHistory : BaseEntity<long>
{
    public long UserId { get; set; }
    public DateTime DownloadedAt { get; set; }

    [MaxLength(300)] public string? VideoTitle { get; set; }

    [MaxLength(1000)] public string? VideoDescription { get; set; }

    [MaxLength(150)] public string? VideoId { get; set; }

    [MaxLength(300)] public string? VideoUrl { get; set; }

    [MaxLength(300)] public string? ImageUrl { get; set; }

    public bool IsArchived { get; set; }
}