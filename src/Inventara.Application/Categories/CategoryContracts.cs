namespace Inventara.Application.Categories;

/// <summary>Category fields returned by the API.</summary>
public sealed record CategoryResponse(Guid Id, string Name, string Description);

/// <summary>Fields used to create a category.</summary>
public sealed record CreateCategoryRequest
{
    /// <summary>Unique category name.</summary>
    public required string Name { get; init; }

    /// <summary>Category description.</summary>
    public required string Description { get; init; }
}

/// <summary>Fields used to replace a category.</summary>
public sealed record UpdateCategoryRequest
{
    /// <summary>Unique category name.</summary>
    public required string Name { get; init; }

    /// <summary>Category description.</summary>
    public required string Description { get; init; }
}
