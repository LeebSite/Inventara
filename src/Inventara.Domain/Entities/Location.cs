using Inventara.Domain.Common;

namespace Inventara.Domain.Entities;

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Building { get; set; } = string.Empty;

    public string Floor { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
