using FluentValidation;
using Inventara.Application.AuditLogs;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;

namespace Inventara.Application.Assets;

public sealed class AssetService(
    IAssetRepository assets,
    IEntityRepository<Category> categories,
    IEntityRepository<Location> locations,
    IEntityRepository<Vendor> vendors,
    IValidator<AssetListQuery> listQueryValidator,
    IValidator<CreateAssetRequest> createValidator,
    IValidator<UpdateAssetRequest> updateValidator,
    IValidator<UploadAssetPhotoRequest> photoValidator,
    IAssetPhotoStorage photoStorage,
    IAssetExcelExporter excelExporter,
    IAuditLogService auditLogs,
    ITransactionManager transactions) : IAssetService
{
    public async Task<PagedResponse<AssetResponse>> GetPageAsync(
        AssetListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var result = await assets.GetAssetPageAsync(
            CreateFilter(query),
            query.Page,
            query.PageSize,
            ResolveSortField(query.SortBy),
            query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        return new PagedResponse<AssetResponse>(
            result.Items.Select(ToResponse).ToArray(),
            result.TotalItems,
            result.TotalPages,
            result.Page,
            result.PageSize);
    }

    public async Task<AssetDetailResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await assets.GetDetailsAsync(id, cancellationToken);
        return result is null
            ? throw new EntityNotFoundException("Asset not found.")
            : new AssetDetailResponse(
                ToResponse(result.Asset),
                result.MostRecentLoan is null ? null : ToResponse(result.MostRecentLoan));
    }

    public async Task<AssetResponse> CreateAsync(
        Guid actorId,
        CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        AssetResponse? response = null;
        await transactions.ExecuteAsync(async token =>
        {
            var category = await GetCategoryAsync(request.CategoryId, token);
            await EnsureLocationExistsAsync(request.LocationId, token);
            await EnsureVendorExistsAsync(request.VendorId, token);

            var asset = new Asset
            {
                Name = request.Name.Trim(),
                CategoryId = request.CategoryId,
                LocationId = request.LocationId,
                VendorId = request.VendorId,
                SerialNumber = NormalizeOptional(request.SerialNumber),
                PurchaseDate = request.PurchaseDate.UtcDateTime,
                PurchasePrice = request.PurchasePrice,
                Condition = request.Condition,
                Status = Inventara.Domain.Enums.AssetStatus.Available,
                Notes = NormalizeOptional(request.Notes)
            };

            await assets.AddWithGeneratedCodeAsync(
                asset,
                GetAssetCodePrefix(category.Name),
                token);
            await auditLogs.AddAsync(
                actorId,
                "Asset",
                asset.Id,
                "Create",
                null,
                ToAuditValues(asset),
                token);
            await assets.SaveChangesAsync(token);
            response = await GetResponseAsync(asset.Id, token);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("The asset was not created.");
    }

    public async Task<AssetResponse> UpdateAsync(
        Guid id,
        Guid actorId,
        UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var asset = await assets.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Asset not found.");
        var oldValues = ToAuditValues(asset);

        await EnsureCategoryExistsAsync(request.CategoryId, cancellationToken);
        await EnsureLocationExistsAsync(request.LocationId, cancellationToken);
        await EnsureVendorExistsAsync(request.VendorId, cancellationToken);

        asset.Name = request.Name.Trim();
        asset.CategoryId = request.CategoryId;
        asset.LocationId = request.LocationId;
        asset.VendorId = request.VendorId;
        asset.SerialNumber = NormalizeOptional(request.SerialNumber);
        asset.PurchaseDate = request.PurchaseDate.UtcDateTime;
        asset.PurchasePrice = request.PurchasePrice;
        asset.Condition = request.Condition;
        asset.Notes = NormalizeOptional(request.Notes);

        assets.Update(asset);
        await auditLogs.AddAsync(
            actorId,
            "Asset",
            asset.Id,
            "Update",
            oldValues,
            ToAuditValues(asset),
            cancellationToken);
        await assets.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var asset = await assets.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Asset not found.");

        var oldValues = ToAuditValues(asset);
        asset.IsDeleted = true;
        assets.Update(asset);
        await auditLogs.AddAsync(
            actorId,
            "Asset",
            asset.Id,
            "Delete",
            oldValues,
            ToAuditValues(asset),
            cancellationToken);
        await assets.SaveChangesAsync(cancellationToken);
    }

    public async Task<AssetResponse> UploadPhotoAsync(
        Guid id,
        Guid actorId,
        UploadAssetPhotoRequest request,
        CancellationToken cancellationToken)
    {
        await photoValidator.ValidateAndThrowAsync(request, cancellationToken);
        var asset = await assets.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Asset not found.");
        var oldValues = ToAuditValues(asset);
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        asset.PhotoUrl = await photoStorage.SaveAsync(
            request.Content,
            extension,
            cancellationToken);

        assets.Update(asset);
        await auditLogs.AddAsync(
            actorId,
            "Asset",
            asset.Id,
            "Update",
            oldValues,
            ToAuditValues(asset),
            cancellationToken);
        await assets.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(id, cancellationToken);
    }

    public async Task<byte[]> ExportAsync(
        AssetListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var records = await assets.GetAssetsForExportAsync(
            CreateFilter(query),
            ResolveSortField(query.SortBy),
            query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        var rows = records.Select(record => new AssetExportRow(
            record.Asset.AssetCode,
            record.Asset.Name,
            record.CategoryName,
            record.LocationName,
            record.VendorName,
            record.Asset.SerialNumber,
            record.Asset.PurchaseDate,
            record.Asset.PurchasePrice,
            record.Asset.Condition,
            record.Asset.Status,
            record.Asset.PhotoUrl,
            record.Asset.Notes)).ToArray();
        return excelExporter.Export(rows);
    }

    private async Task<Category> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        await categories.GetByIdAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException("Category not found.");

    private async Task EnsureCategoryExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        _ = await GetCategoryAsync(id, cancellationToken);
    }

    private async Task EnsureLocationExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await locations.GetByIdAsync(id, cancellationToken) is null)
        {
            throw new EntityNotFoundException("Location not found.");
        }
    }

    private async Task EnsureVendorExistsAsync(Guid? id, CancellationToken cancellationToken)
    {
        if (id.HasValue && await vendors.GetByIdAsync(id.Value, cancellationToken) is null)
        {
            throw new EntityNotFoundException("Vendor not found.");
        }
    }

    private async Task<AssetResponse> GetResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        var details = await assets.GetDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The saved asset could not be loaded.");
        return ToResponse(details.Asset);
    }

    private static AssetFilter CreateFilter(AssetListQuery query) =>
        new(query.Search?.Trim(), query.Status, query.CategoryId, query.LocationId);

    private static string ResolveSortField(string? sortBy) =>
        ServiceHelpers.ResolveSortField(
            sortBy,
            nameof(Asset.AssetCode),
            ("id", nameof(Asset.Id)),
            ("assetCode", nameof(Asset.AssetCode)),
            ("name", nameof(Asset.Name)),
            ("purchaseDate", nameof(Asset.PurchaseDate)),
            ("purchasePrice", nameof(Asset.PurchasePrice)),
            ("condition", nameof(Asset.Condition)),
            ("status", nameof(Asset.Status)),
            ("createdAt", nameof(Asset.CreatedAt)));

    private static string GetAssetCodePrefix(string categoryName)
    {
        var normalizedName = categoryName.ToUpperInvariant();
        var categoryCode = new string(normalizedName
            .Where(character => character is >= 'A' and <= 'Z' or >= '0' and <= '9')
            .Take(3)
            .ToArray())
            .PadRight(3, 'X');
        return $"INV-{categoryCode}";
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AssetAuditValues ToAuditValues(Asset asset) =>
        new(
            asset.AssetCode,
            asset.Name,
            asset.CategoryId,
            asset.LocationId,
            asset.VendorId,
            asset.SerialNumber,
            asset.PurchaseDate,
            asset.PurchasePrice,
            asset.Condition.ToString(),
            asset.Status.ToString(),
            asset.PhotoUrl,
            asset.Notes,
            asset.IsDeleted);

    private static AssetResponse ToResponse(AssetReadRecord record) =>
        new(
            record.Asset.Id,
            record.Asset.AssetCode,
            record.Asset.Name,
            record.Asset.CategoryId,
            record.CategoryName,
            record.Asset.LocationId,
            record.LocationName,
            record.Asset.VendorId,
            record.VendorName,
            record.Asset.SerialNumber,
            ToDateTimeOffset(record.Asset.PurchaseDate),
            record.Asset.PurchasePrice,
            record.Asset.Condition,
            record.Asset.Status,
            record.Asset.PhotoUrl,
            record.Asset.Notes);

    private static AssetRecentLoanResponse ToResponse(AssetRecentLoanRecord loan) =>
        new(
            loan.Id,
            loan.BorrowerId,
            ToDateTimeOffset(loan.RequestedAt),
            ToDateTimeOffset(loan.LoanDate),
            ToDateTimeOffset(loan.DueDate),
            loan.ReturnDate.HasValue ? ToDateTimeOffset(loan.ReturnDate.Value) : null,
            loan.Status,
            loan.Purpose);

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);
}
