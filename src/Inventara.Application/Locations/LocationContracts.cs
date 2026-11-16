namespace Inventara.Application.Locations;

/// <summary>Location fields returned by the API.</summary>
public sealed record LocationResponse(
    Guid Id,
    string Name,
    string Building,
    string Floor,
    string Description);

/// <summary>Fields used to create a location.</summary>
public sealed record CreateLocationRequest
{
    /// <summary>Location name.</summary>
    public required string Name { get; init; }

    /// <summary>Building name.</summary>
    public required string Building { get; init; }

    /// <summary>Floor identifier.</summary>
    public required string Floor { get; init; }

    /// <summary>Location description.</summary>
    public required string Description { get; init; }
}

/// <summary>Fields used to replace a location.</summary>
public sealed record UpdateLocationRequest
{
    /// <summary>Location name.</summary>
    public required string Name { get; init; }

    /// <summary>Building name.</summary>
    public required string Building { get; init; }

    /// <summary>Floor identifier.</summary>
    public required string Floor { get; init; }

    /// <summary>Location description.</summary>
    public required string Description { get; init; }
}
