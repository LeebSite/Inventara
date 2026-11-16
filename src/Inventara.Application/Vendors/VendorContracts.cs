namespace Inventara.Application.Vendors;

/// <summary>Vendor fields returned by the API.</summary>
public sealed record VendorResponse(
    Guid Id,
    string Name,
    string ContactPerson,
    string Email,
    string Phone,
    string Address);

/// <summary>Fields used to create a vendor.</summary>
public sealed record CreateVendorRequest
{
    /// <summary>Vendor name.</summary>
    public required string Name { get; init; }

    /// <summary>Vendor contact person's name.</summary>
    public required string ContactPerson { get; init; }

    /// <summary>Vendor email address.</summary>
    public required string Email { get; init; }

    /// <summary>Vendor phone number.</summary>
    public required string Phone { get; init; }

    /// <summary>Vendor postal address.</summary>
    public required string Address { get; init; }
}

/// <summary>Fields used to replace a vendor.</summary>
public sealed record UpdateVendorRequest
{
    /// <summary>Vendor name.</summary>
    public required string Name { get; init; }

    /// <summary>Vendor contact person's name.</summary>
    public required string ContactPerson { get; init; }

    /// <summary>Vendor email address.</summary>
    public required string Email { get; init; }

    /// <summary>Vendor phone number.</summary>
    public required string Phone { get; init; }

    /// <summary>Vendor postal address.</summary>
    public required string Address { get; init; }
}
