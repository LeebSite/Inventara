using Inventara.Domain.Common;
using Inventara.Domain.Enums;

namespace Inventara.Domain.Entities;

public class Maintenance : BaseEntity
{
    public Guid AssetId { get; set; }

    public Guid ReportedById { get; set; }

    public DateTime ReportedAt { get; set; }

    public string Description { get; set; } = string.Empty;

    public MaintenanceStatus Status { get; set; }

    public decimal? Cost { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime FinishedAt { get; set; }

    public Guid? VendorId { get; set; }
}
