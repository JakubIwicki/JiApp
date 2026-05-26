// ReSharper disable PropertyCanBeMadeInitOnly.Global

using System.ComponentModel.DataAnnotations;

namespace JiApp.Common.Models;

public class YoutubeSearchHistory : BaseEntity<long>
{
    public long UserId { get; set; }
    public DateTime? SearchedAt { get; set; }

    [MaxLength(100)] public string? SearchText { get; set; }

    public bool IsArchived { get; set; }
}