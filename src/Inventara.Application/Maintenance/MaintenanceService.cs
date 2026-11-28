using FluentValidation;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;

namespace Inventara.Application.Maintenance;

public sealed class MaintenanceService(
    IMaintenanceRepository maintenances,
    ILoanRepository loans,
    IEntityRepository<Asset> assets,
    IEntityRepository<Vendor> vendors,
    IValidator<MaintenanceListQuery> listQueryValidator,
    IValidator<CreateMaintenanceRequest> createValidator,
    IValidator<UpdateMaintenanceRequest> updateValidator,
    ITransactionManager transactions) : IMaintenanceService
{
    public async Task<PagedResponse<MaintenanceResponse>> GetPageAsync(
        MaintenanceListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var page = await maintenances.GetPageAsync(
            new MaintenanceFilter(query.Status, query.AssetId),
            query.Page,
            query.PageSize,
            ResolveSortField(query.SortBy),
            query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase),
            cancellationToken);
        return ServiceHelpers.MapPage(page, ToResponse);
    }

    public async Task<MaintenanceResponse> CreateAsync(
        Guid reporterId,
        CreateMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        MaintenanceResponse? response = null;
        await transactions.ExecuteAsync(async token =>
        {
            var asset = await assets.GetByIdAsync(request.AssetId, token)
                ?? throw new EntityNotFoundException("Asset not found.");
            if (await loans.HasActiveLoanAsync(asset.Id, token))
            {
                throw new ConflictException("An asset with an active loan cannot be sent to maintenance.");
            }

            if (await maintenances.HasActiveTicketAsync(asset.Id, token))
            {
                throw new ConflictException("The asset already has an active maintenance ticket.");
            }

            await EnsureVendorExistsAsync(request.VendorId, token);

            var reportedAt = DateTime.UtcNow;
            var maintenance = new Domain.Entities.Maintenance
            {
                AssetId = asset.Id,
                ReportedById = reporterId,
                ReportedAt = reportedAt,
                Description = request.Description.Trim(),
                Status = MaintenanceStatus.Reported,
                StartedAt = reportedAt,
                FinishedAt = reportedAt,
                VendorId = request.VendorId
            };

            asset.Status = AssetStatus.Maintenance;
            assets.Update(asset);
            await maintenances.AddAsync(maintenance, token);
            await maintenances.SaveChangesAsync(token);
            response = await GetResponseAsync(maintenance.Id, token);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("The maintenance ticket was not created.");
    }

    public async Task<MaintenanceResponse> UpdateAsync(
        Guid id,
        UpdateMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        await transactions.ExecuteAsync(async token =>
        {
            var maintenance = await GetActiveMaintenanceAsync(id, token);
            maintenance.Status = request.Status;
            maintenance.Cost = request.Cost;
            maintenance.FinishedAt = request.FinishedAt.UtcDateTime;
            maintenances.Update(maintenance);
            await maintenances.SaveChangesAsync(token);
        }, cancellationToken);

        return await GetResponseAsync(id, cancellationToken);
    }

    public async Task<MaintenanceResponse> CompleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await transactions.ExecuteAsync(async token =>
        {
            var maintenance = await GetActiveMaintenanceAsync(id, token);
            var asset = await assets.GetByIdAsync(maintenance.AssetId, token)
                ?? throw new EntityNotFoundException("Asset not found.");

            maintenance.Status = MaintenanceStatus.Done;
            maintenance.FinishedAt = DateTime.UtcNow;
            asset.Status = AssetStatus.Available;
            maintenances.Update(maintenance);
            assets.Update(asset);
            await maintenances.SaveChangesAsync(token);
        }, cancellationToken);

        return await GetResponseAsync(id, cancellationToken);
    }

    private async Task EnsureVendorExistsAsync(Guid? vendorId, CancellationToken cancellationToken)
    {
        if (vendorId.HasValue
            && await vendors.GetByIdAsync(vendorId.Value, cancellationToken) is null)
        {
            throw new EntityNotFoundException("Vendor not found.");
        }
    }

    private async Task<Domain.Entities.Maintenance> GetActiveMaintenanceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var maintenance = await maintenances.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Maintenance ticket not found.");
        if (maintenance.Status is MaintenanceStatus.Done or MaintenanceStatus.Cancelled)
        {
            throw new ConflictException("A completed or cancelled maintenance ticket cannot be changed.");
        }

        return maintenance;
    }

    private async Task<MaintenanceResponse> GetResponseAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var record = await maintenances.GetDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The saved maintenance ticket could not be loaded.");
        return ToResponse(record);
    }

    private static string ResolveSortField(string? sortBy) =>
        ServiceHelpers.ResolveSortField(
            sortBy,
            nameof(Domain.Entities.Maintenance.ReportedAt),
            ("id", nameof(Domain.Entities.Maintenance.Id)),
            ("status", nameof(Domain.Entities.Maintenance.Status)),
            ("reportedAt", nameof(Domain.Entities.Maintenance.ReportedAt)),
            ("startedAt", nameof(Domain.Entities.Maintenance.StartedAt)),
            ("finishedAt", nameof(Domain.Entities.Maintenance.FinishedAt)),
            ("cost", nameof(Domain.Entities.Maintenance.Cost)),
            ("createdAt", nameof(Domain.Entities.Maintenance.CreatedAt)));

    private static MaintenanceResponse ToResponse(MaintenanceReadRecord record) =>
        new(
            record.Maintenance.Id,
            record.Maintenance.AssetId,
            record.AssetCode,
            record.AssetName,
            record.Maintenance.ReportedById,
            ServiceHelpers.ToUtcOffset(record.Maintenance.ReportedAt),
            record.Maintenance.Description,
            record.Maintenance.Status,
            record.Maintenance.Cost,
            ServiceHelpers.ToUtcOffset(record.Maintenance.StartedAt),
            ServiceHelpers.ToUtcOffset(record.Maintenance.FinishedAt),
            record.Maintenance.VendorId);
}
