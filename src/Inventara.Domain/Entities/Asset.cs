using Inventara.Domain.Common;
using Inventara.Domain.Enums;

namespace Inventara.Domain.Entities;

public class Asset : BaseEntity
{
    public string AssetCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? VendorId { get; set; }

    public string? SerialNumber { get; set; }

    public DateTime PurchaseDate { get; set; }

    public decimal PurchasePrice { get; set; }

    public AssetCondition Condition { get; set; }

    public AssetStatus Status { get; set; }

    public string? PhotoUrl { get; set; }

    public string? Notes { get; set; }
}
