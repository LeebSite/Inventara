using System.Linq.Expressions;
using Inventara.Application.AuditLogs;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Loans;
using Inventara.Application.Persistence;
using Inventara.Application.Validation;
using Inventara.Domain.Common;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;

namespace Inventara.Tests;

public sealed class LoanServiceTests
{
    [Fact]
    public async Task CreateAsync_AvailableAsset_CreatesPendingLoan()
    {
        var asset = new Asset { Status = AssetStatus.Available };
        var borrower = new User { FullName = "Borrower" };
        var repository = new FakeLoanRepository(assets: [asset], users: [borrower]);
        var transactions = new FakeTransactionManager();
        var service = CreateService(
            loanRepository: repository,
            assets: [asset],
            transactions: transactions);

        var result = await service.CreateAsync(
            borrower.Id,
            CreateRequest(asset.Id),
            CancellationToken.None);

        var loan = Assert.Single(repository.Items);
        Assert.Equal(LoanStatus.Pending, loan.Status);
        Assert.Equal(loan.RequestedAt, loan.LoanDate);
        Assert.Equal(loan.Id, result.Id);
        Assert.Equal(1, transactions.ExecutionCount);
    }

    [Fact]
    public async Task CreateAsync_UnavailableAsset_ThrowsConflict()
    {
        var asset = new Asset { Status = AssetStatus.OnLoan };
        var service = CreateService(assets: [asset]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(Guid.NewGuid(), CreateRequest(asset.Id), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_PastDueDate_FailsValidation()
    {
        var service = CreateService();
        var request = CreateRequest(Guid.NewGuid()) with
        {
            DueDate = DateTimeOffset.UtcNow.AddDays(-1)
        };

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.CreateAsync(Guid.NewGuid(), request, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveAsync_PendingLoan_UpdatesLoanAssetAndWritesAuditInTransaction()
    {
        var asset = new Asset { Status = AssetStatus.Available };
        var borrower = new User { FullName = "Borrower" };
        var loan = CreateLoan(asset.Id, borrower.Id);
        var repository = new FakeLoanRepository([loan], [asset], [borrower]);
        var auditLogs = new FakeAuditLogService();
        var transactions = new FakeTransactionManager();
        var service = CreateService(
            loanRepository: repository,
            assets: [asset],
            auditLogs: auditLogs,
            transactions: transactions);

        var result = await service.ApproveAsync(loan.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(LoanStatus.OnLoan, loan.Status);
        Assert.Equal(AssetStatus.OnLoan, asset.Status);
        Assert.Equal(LoanStatus.OnLoan, result.Status);
        Assert.Equal(1, transactions.ExecutionCount);
        Assert.Equal("Approve", Assert.Single(auditLogs.Entries).Action);
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApprovedLoan_ThrowsConflict()
    {
        var loan = CreateLoan(Guid.NewGuid(), Guid.NewGuid());
        loan.Status = LoanStatus.OnLoan;
        var service = CreateService(loans: [loan]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.ApproveAsync(loan.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task RejectAsync_RequiredReasonMissing_FailsValidation()
    {
        var loan = CreateLoan(Guid.NewGuid(), Guid.NewGuid());
        var service = CreateService(loans: [loan]);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.RejectAsync(
                loan.Id,
                Guid.NewGuid(),
                new RejectLoanRequest { Reason = " " },
                CancellationToken.None));
    }

    [Fact]
    public async Task RejectAsync_PendingLoan_RecordsReasonAndAudit()
    {
        var loan = CreateLoan(Guid.NewGuid(), Guid.NewGuid());
        var auditLogs = new FakeAuditLogService();
        var service = CreateService(loans: [loan], auditLogs: auditLogs);

        var result = await service.RejectAsync(
            loan.Id,
            Guid.NewGuid(),
            new RejectLoanRequest { Reason = "Not available" },
            CancellationToken.None);

        Assert.Equal(LoanStatus.Rejected, result.Status);
        Assert.Equal("Not available", loan.RejectionReason);
        Assert.Equal("Reject", Assert.Single(auditLogs.Entries).Action);
    }

    [Fact]
    public async Task ReturnAsync_GoodCondition_MakesAssetAvailable()
    {
        var asset = new Asset { Status = AssetStatus.OnLoan };
        var loan = CreateLoan(asset.Id, Guid.NewGuid());
        loan.Status = LoanStatus.OnLoan;
        var maintenanceRepository = new FakeRepository<Maintenance>();
        var service = CreateService(
            loans: [loan],
            assets: [asset],
            maintenanceRepository: maintenanceRepository);

        var result = await service.ReturnAsync(
            loan.Id,
            Guid.NewGuid(),
            new ReturnLoanRequest { ReturnCondition = AssetCondition.Good },
            CancellationToken.None);

        Assert.Equal(LoanStatus.Returned, result.Status);
        Assert.Equal(AssetStatus.Available, asset.Status);
        Assert.Empty(maintenanceRepository.Items);
    }

    [Fact]
    public async Task ReturnAsync_DamagedCondition_CreatesReportedMaintenanceAndAudit()
    {
        var asset = new Asset { Status = AssetStatus.OnLoan };
        var loan = CreateLoan(asset.Id, Guid.NewGuid());
        loan.Status = LoanStatus.OnLoan;
        var reporterId = Guid.NewGuid();
        var auditLogs = new FakeAuditLogService();
        var transactions = new FakeTransactionManager();
        var maintenanceRepository = new FakeRepository<Maintenance>();
        var service = CreateService(
            loans: [loan],
            assets: [asset],
            auditLogs: auditLogs,
            transactions: transactions,
            maintenanceRepository: maintenanceRepository);

        await service.ReturnAsync(
            loan.Id,
            reporterId,
            new ReturnLoanRequest { ReturnCondition = AssetCondition.MinorDamage },
            CancellationToken.None);

        var maintenance = Assert.Single(maintenanceRepository.Items);
        Assert.Equal(AssetStatus.Maintenance, asset.Status);
        Assert.Equal(MaintenanceStatus.Reported, maintenance.Status);
        Assert.Equal(reporterId, maintenance.ReportedById);
        Assert.Equal(maintenance.ReportedAt, maintenance.StartedAt);
        Assert.Equal(maintenance.StartedAt, maintenance.FinishedAt);
        Assert.Equal("Return", Assert.Single(auditLogs.Entries).Action);
        Assert.Equal(1, transactions.ExecutionCount);
    }

    [Fact]
    public async Task GetOverduePageAsync_OnLoanPastDue_ReportsOverdueWithoutChangingLoan()
    {
        var overdueLoan = CreateLoan(Guid.NewGuid(), Guid.NewGuid());
        overdueLoan.Status = LoanStatus.OnLoan;
        overdueLoan.DueDate = DateTime.UtcNow.AddDays(-1);
        var service = CreateService(loans: [overdueLoan]);

        var page = await service.GetOverduePageAsync(new LoanListQuery(), CancellationToken.None);

        Assert.Equal(LoanStatus.Overdue, Assert.Single(page.Items).Status);
        Assert.Equal(LoanStatus.OnLoan, overdueLoan.Status);
    }

    [Fact]
    public async Task GetByIdAsync_NonOwnerWithoutStaffRole_ThrowsForbidden()
    {
        var loan = CreateLoan(Guid.NewGuid(), Guid.NewGuid());
        var service = CreateService(loans: [loan]);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetByIdAsync(loan.Id, Guid.NewGuid(), false, CancellationToken.None));
    }

    private static CreateLoanRequest CreateRequest(Guid assetId) =>
        new()
        {
            AssetId = assetId,
            DueDate = DateTimeOffset.UtcNow.AddDays(2),
            Purpose = "Project work"
        };

    private static Loan CreateLoan(Guid assetId, Guid borrowerId) =>
        new()
        {
            AssetId = assetId,
            BorrowerId = borrowerId,
            RequestedAt = DateTime.UtcNow,
            LoanDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(2),
            Status = LoanStatus.Pending,
            Purpose = "Project work"
        };

    private static LoanService CreateService(
        FakeLoanRepository? loanRepository = null,
        IEnumerable<Loan>? loans = null,
        IEnumerable<Asset>? assets = null,
        FakeAuditLogService? auditLogs = null,
        FakeTransactionManager? transactions = null,
        FakeRepository<Maintenance>? maintenanceRepository = null)
    {
        var assetItems = assets?.ToArray() ?? [];
        var loanRepo = loanRepository ?? new FakeLoanRepository(loans, assetItems);
        maintenanceRepository ??= new FakeRepository<Maintenance>();
        return new LoanService(
            loanRepo,
            new FakeRepository<Asset>(assetItems),
            maintenanceRepository,
            auditLogs ?? new FakeAuditLogService(),
            transactions ?? new FakeTransactionManager(),
            new CreateLoanRequestValidator(),
            new LoanListQueryValidator(),
            new RejectLoanRequestValidator(),
            new ReturnLoanRequestValidator());
    }

    private sealed class FakeLoanRepository(
        IEnumerable<Loan>? loans = null,
        IEnumerable<Asset>? assets = null,
        IEnumerable<User>? users = null) : FakeRepository<Loan>(loans), ILoanRepository
    {
        private readonly List<Asset> _assets = assets?.ToList() ?? [];
        private readonly List<User> _users = users?.ToList() ?? [];

        public Task<bool> HasActiveLoanAsync(Guid assetId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(loan =>
                !loan.IsDeleted
                && loan.AssetId == assetId
                && loan.Status is LoanStatus.Approved or LoanStatus.OnLoan));

        public Task<PagedResult<LoanReadRecord>> GetPageAsync(
            LoanFilter filter,
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken)
        {
            var filtered = Items.Where(loan =>
                !loan.IsDeleted
                && (!filter.BorrowerId.HasValue || loan.BorrowerId == filter.BorrowerId)
                && (!filter.Status.HasValue || loan.Status == filter.Status)
                && (!filter.RequestedFrom.HasValue || loan.RequestedAt >= filter.RequestedFrom)
                && (!filter.RequestedTo.HasValue || loan.RequestedAt <= filter.RequestedTo)
                && (!filter.LoanFrom.HasValue || loan.LoanDate >= filter.LoanFrom)
                && (!filter.LoanTo.HasValue || loan.LoanDate <= filter.LoanTo)
                && (!filter.DueFrom.HasValue || loan.DueDate >= filter.DueFrom)
                && (!filter.DueTo.HasValue || loan.DueDate <= filter.DueTo)
                && (!filter.OverdueBefore.HasValue
                    || (loan.Status == LoanStatus.OnLoan && loan.DueDate < filter.OverdueBefore)))
                .OrderBy(loan => loan.RequestedAt)
                .Select(ToReadRecord)
                .ToArray();
            var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
            var totalPages = (int)Math.Ceiling(filtered.Length / (double)pageSize);
            return Task.FromResult(new PagedResult<LoanReadRecord>(
                items,
                filtered.Length,
                totalPages,
                page,
                pageSize));
        }

        public Task<LoanReadRecord?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
        {
            var loan = Items.FirstOrDefault(candidate => candidate.Id == id && !candidate.IsDeleted);
            return Task.FromResult(loan is null ? null : ToReadRecord(loan));
        }

        private LoanReadRecord ToReadRecord(Loan loan)
        {
            var asset = _assets.FirstOrDefault(candidate => candidate.Id == loan.AssetId);
            var user = _users.FirstOrDefault(candidate => candidate.Id == loan.BorrowerId);
            return new LoanReadRecord(
                loan,
                asset?.AssetCode ?? "INV-LAP-0001",
                asset?.Name ?? "Laptop",
                user?.FullName ?? "Borrower");
        }
    }

    private class FakeRepository<TEntity>(IEnumerable<TEntity>? entities = null)
        : IEntityRepository<TEntity>
        where TEntity : BaseEntity
    {
        public List<TEntity> Items { get; } = entities?.ToList() ?? [];

        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(entity => entity.Id == id && !entity.IsDeleted));

        public Task<bool> AnyIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(predicate.Compile()));

        public Task<PagedResult<TEntity>> GetPageAsync(
            Expression<Func<TEntity, bool>> predicate,
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<TEntity>([], 0, 0, page, pageSize));

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(TEntity entity)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(1);
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<AuditLogEntry> Entries { get; } = [];

        public Task AddAsync(
            Guid? userId,
            string entityName,
            Guid entityId,
            string action,
            object? oldValues,
            object? newValues,
            CancellationToken cancellationToken)
        {
            Entries.Add(new AuditLogEntry(userId, entityName, entityId, action, oldValues, newValues));
            return Task.CompletedTask;
        }

        public Task<PagedResponse<AuditLogResponse>> GetPageAsync(
            AuditLogQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResponse<AuditLogResponse>([], 0, 0, query.Page, query.PageSize));
    }

    private sealed record AuditLogEntry(
        Guid? UserId,
        string EntityName,
        Guid EntityId,
        string Action,
        object? OldValues,
        object? NewValues);

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public int ExecutionCount { get; private set; }

        public Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return operation(cancellationToken);
        }
    }
}
