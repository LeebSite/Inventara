using FluentAssertions;
using Inventara.Application.AuditLogs;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Loans;
using Inventara.Application.Persistence;
using Inventara.Application.Validation;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;
using Inventara.Infrastructure.Data;
using Inventara.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Tests;

public sealed class LoanServiceInMemoryTests
{
    [Fact]
    public async Task ApproveAsync_PendingLoan_ChangesLoanStatusToOnLoan()
    {
        await using var context = CreateContext();
        var (loan, _) = await SeedLoanAsync(context, LoanStatus.Pending);
        var service = CreateService(context);

        await service.ApproveAsync(loan.Id, Guid.NewGuid(), CancellationToken.None);

        var savedLoan = await context.Loans.AsNoTracking().SingleAsync();
        savedLoan.Status.Should().Be(LoanStatus.OnLoan);
    }

    [Fact]
    public async Task ApproveAsync_PendingLoan_ChangesAssetStatusToOnLoan()
    {
        await using var context = CreateContext();
        var (loan, _) = await SeedLoanAsync(context, LoanStatus.Pending);
        var service = CreateService(context);

        await service.ApproveAsync(loan.Id, Guid.NewGuid(), CancellationToken.None);

        var savedAsset = await context.Assets.AsNoTracking().SingleAsync();
        savedAsset.Status.Should().Be(AssetStatus.OnLoan);
    }

    [Theory]
    [InlineData(LoanStatus.Approved)]
    [InlineData(LoanStatus.OnLoan)]
    public async Task CreateAsync_AssetHasActiveLoan_RejectsSecondRequest(LoanStatus activeStatus)
    {
        await using var context = CreateContext();
        var (_, asset) = await SeedLoanAsync(context, activeStatus);
        var service = CreateService(context);
        var request = new CreateLoanRequest
        {
            AssetId = asset.Id,
            DueDate = DateTimeOffset.UtcNow.AddDays(2),
            Purpose = "Second request"
        };

        var act = () => service.CreateAsync(Guid.NewGuid(), request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        (await context.Loans.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ReturnAsync_DamagedCondition_CreatesMaintenanceTicket()
    {
        await using var context = CreateContext();
        var (loan, asset) = await SeedLoanAsync(context, LoanStatus.OnLoan);
        var reporterId = Guid.NewGuid();
        var service = CreateService(context);

        await service.ReturnAsync(
            loan.Id,
            reporterId,
            new ReturnLoanRequest { ReturnCondition = AssetCondition.MinorDamage },
            CancellationToken.None);

        var maintenance = await context.Maintenances.AsNoTracking().SingleAsync();
        maintenance.AssetId.Should().Be(asset.Id);
        maintenance.ReportedById.Should().Be(reporterId);
        maintenance.Status.Should().Be(MaintenanceStatus.Reported);
    }

    [Fact]
    public async Task ReturnAsync_DamagedCondition_ChangesAssetStatusToMaintenance()
    {
        await using var context = CreateContext();
        var (loan, _) = await SeedLoanAsync(context, LoanStatus.OnLoan);
        var service = CreateService(context);

        await service.ReturnAsync(
            loan.Id,
            Guid.NewGuid(),
            new ReturnLoanRequest { ReturnCondition = AssetCondition.MajorDamage },
            CancellationToken.None);

        var savedAsset = await context.Assets.AsNoTracking().SingleAsync();
        savedAsset.Status.Should().Be(AssetStatus.Maintenance);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Loan Loan, Asset Asset)> SeedLoanAsync(
        AppDbContext context,
        LoanStatus status)
    {
        var asset = new Asset
        {
            AssetCode = "INV-LAP-0001",
            Name = "Test laptop",
            CategoryId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            PurchaseDate = DateTime.UtcNow.Date,
            PurchasePrice = 1200,
            Condition = AssetCondition.Good,
            Status = AssetStatus.Available
        };
        var loan = new Loan
        {
            AssetId = asset.Id,
            BorrowerId = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow.AddMinutes(-1),
            LoanDate = DateTime.UtcNow.AddMinutes(-1),
            DueDate = DateTime.UtcNow.AddDays(2),
            Status = status,
            Purpose = "Test loan"
        };

        context.Assets.Add(asset);
        context.Loans.Add(loan);
        await context.SaveChangesAsync();
        return (loan, asset);
    }

    private static LoanService CreateService(AppDbContext context) =>
        new(
            new LoanRepository(context),
            new EntityRepository<Asset>(context),
            new EntityRepository<Maintenance>(context),
            new AuditLogService(
                new EntityRepository<AuditLog>(context),
                new AuditLogQueryValidator()),
            new InMemoryTransactionManager(),
            new CreateLoanRequestValidator(),
            new LoanListQueryValidator(),
            new RejectLoanRequestValidator(),
            new ReturnLoanRequestValidator());

    private sealed class InMemoryTransactionManager : ITransactionManager
    {
        public Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }
}
