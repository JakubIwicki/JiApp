using JiApp.Common.Models;

namespace JiApp.Scheduler.Domain;

public enum ServiceCategory
{
    MensHaircut,
    WomensHaircut,
    WomensStyling,
    Coloring,
    Treatment,
    Other
}

public sealed class Service : BaseEntity<long>
{
    public long BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ServiceCategory Category { get; set; }
    public int BaseDuration { get; set; }
    public Price BasePrice { get; set; } = new();
    public Board Board { get; set; } = null!;
}