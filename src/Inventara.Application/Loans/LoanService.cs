using FluentValidation;
using Inventara.Application.AuditLogs;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;
using MaintenanceEntity = Inventara.Domain.Entities.Maintenance;

namespace Inventara.Application.Loans;

public sealed class LoanService(
    ILoanRepository loans,
    IEntityRepository<Asset> assets,
    IEntityRepository<MaintenanceEntity> maintenances,
    IAuditLogService auditLogs,
    ITransactionManager transactions,
    IValidator<CreateLoanRequest> createValidator,
    IValidator<LoanListQuery> listQueryValidator,
    IValidator<RejectLoanRequest> rejectValidator,
    IValidator<ReturnLoanRequest> returnValidator) : ILoanService
{
    public async Task<LoanResponse> CreateAsync(
        Guid borrowerId,
        CreateLoanRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        LoanResponse? response = null;
        await transactions.ExecuteAsync(async token =>
        {
            var asset = await assets.GetByIdAsync(request.AssetId, token)
                ?? throw new EntityNotFoundException("Asset not found.");
            if (asset.Status != AssetStatus.Available
                || await loans.HasActiveLoanAsync(asset.Id, token))
            {
                throw new ConflictException("The asset is not available for loan.");
            }

            var requestedAt = DateTime.UtcNow;
            var loan = new Loan
            {
                AssetId = asset.Id,
                BorrowerId = borrowerId,
                RequestedAt = requestedAt,
                LoanDate = requestedAt,
                DueDate = request.DueDate.UtcDateTime,
                Status = LoanStatus.Pending,
                Purpose = request.Purpose.Trim()
            };

            await loans.AddAsync(loan, token);
            await loans.SaveChangesAsync(token);
            response = await GetResponseAsync(loan.Id, token);
        }, cancellationToken);

        return response ?? throw new InvalidOperationException("The loan request was not created.");
    }

    public async Task<PagedResponse<LoanResponse>> GetMyPageAsync(
        Guid borrowerId,
        LoanListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        return await GetPageCoreAsync(
            CreateFilter(query, borrowerId),
            query,
            overdue: false,
            cancellationToken);
    }

    public async Task<PagedResponse<LoanResponse>> GetPageAsync(
        LoanListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        return await GetPageCoreAsync(
            CreateFilter(query, null),
            query,
            overdue: false,
            cancellationToken);
    }

    public async Task<LoanResponse> GetByIdAsync(
        Guid id,
        Guid requesterId,
        bool canViewAll,
        CancellationToken cancellationToken)
    {
        var result = await loans.GetDetailsAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Loan not found.");
        if (!canViewAll && result.Loan.BorrowerId != requesterId)
        {
            throw new ForbiddenException("You are not allowed to view this loan.");
        }

        return ToResponse(result);
    }

    public async Task<LoanResponse> ApproveAsync(
        Guid id,
        Guid approverId,
        CancellationToken cancellationToken)
    {
        await transactions.ExecuteAsync(async token =>
        {
            var loan = await GetPendingLoanAsync(id, token);
            var asset = await assets.GetByIdAsync(loan.AssetId, token)
                ?? throw new EntityNotFoundException("Asset not found.");
            if (asset.Status != AssetStatus.Available
                || await loans.HasActiveLoanAsync(asset.Id, token))
            {
                throw new ConflictException("The asset is not available for loan.");
            }

            var oldValues = ToAuditValues(loan);
            var approvedAt = DateTime.UtcNow;
            loan.Status = LoanStatus.OnLoan;
            loan.ApprovedById = approverId;
            loan.LoanDate = approvedAt;
            asset.Status = AssetStatus.OnLoan;
            loans.Update(loan);
            assets.Update(asset);
            await auditLogs.AddAsync(
                approverId,
                "Loan",
                loan.Id,
                "Approve",
                oldValues,
                ToAuditValues(loan),
                token);
            await loans.SaveChangesAsync(token);
        }, cancellationToken);

        return await GetResponseAsync(id, cancellationToken);
    }

    public async Task<LoanResponse> RejectAsync(
        Guid id,
        Guid approverId,
        RejectLoanRequest request,
        CancellationToken cancellationToken)
    {
        await rejectValidator.ValidateAndThrowAsync(request, cancellationToken);
        await transactions.ExecuteAsync(async token =>
        {
            var loan = await GetPendingLoanAsync(id, token);
            var oldValues = ToAuditValues(loan);
            loan.Status = LoanStatus.Rejected;
            loan.ApprovedById = approverId;
            loan.RejectionReason = request.Reason.Trim();
            loans.Update(loan);
            await auditLogs.AddAsync(
                approverId,
                "Loan",
                loan.Id,
                "Reject",
                oldValues,
                ToAuditValues(loan),
                token);
            await loans.SaveChangesAsync(token);
        }, cancellationToken);

        return await GetResponseAsync(id, cancellationToken);
    }

    public async Task<LoanResponse> ReturnAsync(
        Guid id,
        Guid reporterId,
        ReturnLoanRequest request,
        CancellationToken cancellationToken)
    {
        await returnValidator.ValidateAndThrowAsync(request, cancellationToken);
        await transactions.ExecuteAsync(async token =>
        {
            var loan = await loans.GetByIdAsync(id, token)
                ?? throw new EntityNotFoundException("Loan not found.");
            if (loan.Status != LoanStatus.OnLoan)
            {
                throw new ConflictException("Only a loan currently on loan can be returned.");
            }

            var asset = await assets.GetByIdAsync(loan.AssetId, token)
                ?? throw new EntityNotFoundException("Asset not found.");
            var oldValues = ToAuditValues(loan);
            var returnedAt = DateTime.UtcNow;
            loan.ReturnDate = returnedAt;
            loan.ReturnCondition = request.ReturnCondition;
            loan.Status = LoanStatus.Returned;
            asset.Status = request.ReturnCondition == AssetCondition.Good
                ? AssetStatus.Available
                : AssetStatus.Maintenance;

            if (request.ReturnCondition != AssetCondition.Good)
            {
                await maintenances.AddAsync(
                    new MaintenanceEntity
                    {
                        AssetId = asset.Id,
                        ReportedById = reporterId,
                        ReportedAt = returnedAt,
                        Description = $"Automatically reported after loan return. Return condition: {request.ReturnCondition}.",
                        Status = MaintenanceStatus.Reported,
                        StartedAt = returnedAt,
                        FinishedAt = returnedAt
                    },
                    token);
            }

            loans.Update(loan);
            assets.Update(asset);
            await auditLogs.AddAsync(
                reporterId,
                "Loan",
                loan.Id,
                "Return",
                oldValues,
                ToAuditValues(loan),
                token);
            await loans.SaveChangesAsync(token);
        }, cancellationToken);

        return await GetResponseAsync(id, cancellationToken);
    }

    public async Task<PagedResponse<LoanResponse>> GetOverduePageAsync(
        LoanListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var filter = CreateFilter(query, null) with
        {
            Status = null,
            OverdueBefore = DateTime.UtcNow
        };
        return await GetPageCoreAsync(filter, query, overdue: true, cancellationToken);
    }

    private async Task<PagedResponse<LoanResponse>> GetPageCoreAsync(
        LoanFilter filter,
        LoanListQuery query,
        bool overdue,
        CancellationToken cancellationToken)
    {
        var page = await loans.GetPageAsync(
            filter,
            query.Page,
            query.PageSize,
            ResolveSortField(query.SortBy),
            query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase),
            cancellationToken);
        var items = page.Items
            .Select(record => ToResponse(
                record,
                overdue ? LoanStatus.Overdue : null))
            .ToArray();
        return new PagedResponse<LoanResponse>(
            items,
            page.TotalItems,
            page.TotalPages,
            page.Page,
            page.PageSize);
    }

    private async Task<Loan> GetPendingLoanAsync(Guid id, CancellationToken cancellationToken)
    {
        var loan = await loans.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Loan not found.");
        if (loan.Status != LoanStatus.Pending)
        {
            throw new ConflictException("Only pending loans can be approved or rejected.");
        }

        return loan;
    }

    private async Task<LoanResponse> GetResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await loans.GetDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The saved loan could not be loaded.");
        return ToResponse(result);
    }

    private static LoanFilter CreateFilter(LoanListQuery query, Guid? borrowerId) =>
        new(
            borrowerId,
            query.Status,
            query.RequestedFrom?.UtcDateTime,
            query.RequestedTo?.UtcDateTime,
            query.LoanFrom?.UtcDateTime,
            query.LoanTo?.UtcDateTime,
            query.DueFrom?.UtcDateTime,
            query.DueTo?.UtcDateTime,
            null);

    private static string ResolveSortField(string? sortBy) =>
        ServiceHelpers.ResolveSortField(
            sortBy,
            nameof(Loan.RequestedAt),
            ("id", nameof(Loan.Id)),
            ("requestedAt", nameof(Loan.RequestedAt)),
            ("loanDate", nameof(Loan.LoanDate)),
            ("dueDate", nameof(Loan.DueDate)),
            ("returnDate", nameof(Loan.ReturnDate)),
            ("status", nameof(Loan.Status)),
            ("createdAt", nameof(Loan.CreatedAt)));

    private static LoanResponse ToResponse(
        LoanReadRecord record,
        LoanStatus? statusOverride = null) =>
        new(
            record.Loan.Id,
            record.Loan.AssetId,
            record.AssetCode,
            record.AssetName,
            record.Loan.BorrowerId,
            record.BorrowerName,
            ServiceHelpers.ToUtcOffset(record.Loan.RequestedAt),
            ServiceHelpers.ToUtcOffset(record.Loan.LoanDate),
            ServiceHelpers.ToUtcOffset(record.Loan.DueDate),
            record.Loan.ReturnDate.HasValue
                ? ServiceHelpers.ToUtcOffset(record.Loan.ReturnDate.Value)
                : null,
            statusOverride ?? record.Loan.Status,
            record.Loan.Purpose,
            record.Loan.ApprovedById,
            record.Loan.RejectionReason,
            record.Loan.ReturnCondition);

    private static LoanAuditValues ToAuditValues(Loan loan) =>
        new(
            loan.AssetId,
            loan.BorrowerId,
            loan.RequestedAt,
            loan.LoanDate,
            loan.DueDate,
            loan.ReturnDate,
            loan.Status.ToString(),
            loan.Purpose,
            loan.ApprovedById,
            loan.RejectionReason,
            loan.ReturnCondition?.ToString());
}
