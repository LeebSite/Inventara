using Inventara.Application.Common;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;

namespace Inventara.Application.Persistence;

public interface IAssetRepository : IEntityRepository<Asset>
{
    Task<PagedResult<AssetReadRecord>> GetAssetPageAsync(
        AssetFilter filter,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken);

    Task<AssetDetailsRecord?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AssetReadRecord>> GetAssetsForExportAsync(
        AssetFilter filter,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken);

    Task AddWithGeneratedCodeAsync(
        Asset asset,
        string codePrefix,
        CancellationToken cancellationToken);
}

public sealed record AssetFilter(
    string? Search,
    AssetStatus? Status,
    Guid? CategoryId,
    Guid? LocationId);

public sealed record AssetReadRecord(
    Asset Asset,
    string CategoryName,
    string LocationName,
    string? VendorName);

public sealed record AssetDetailsRecord(
    AssetReadRecord Asset,
    AssetRecentLoanRecord? MostRecentLoan);

public sealed record AssetRecentLoanRecord(
    Guid Id,
    Guid BorrowerId,
    DateTime RequestedAt,
    DateTime LoanDate,
    DateTime DueDate,
    DateTime? ReturnDate,
    LoanStatus Status,
    string Purpose);
