using Inventara.Application.Common;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Infrastructure.Persistence;

public sealed class LoanRepository : EntityRepository<Loan>, ILoanRepository
{
    private readonly AppDbContext _dbContext;

    public LoanRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HasActiveLoanAsync(Guid assetId, CancellationToken cancellationToken) =>
        _dbContext.Loans.AnyAsync(
            loan => loan.AssetId == assetId
                && (loan.Status == Domain.Enums.LoanStatus.Approved
                    || loan.Status == Domain.Enums.LoanStatus.OnLoan),
            cancellationToken);

    public async Task<PagedResult<LoanReadRecord>> GetPageAsync(
        LoanFilter filter,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var loans = BuildLoanQuery(filter);
        var totalItems = await loans.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            return new PagedResult<LoanReadRecord>([], totalItems, totalPages, page, pageSize);
        }

        var orderedLoans = ApplyOrdering(loans, sortBy, descending)
            .ThenBy(loan => loan.Id)
            .Skip((int)offset)
            .Take(pageSize);
        var items = await BuildReadQuery(orderedLoans).ToListAsync(cancellationToken);
        return new PagedResult<LoanReadRecord>(items, totalItems, totalPages, page, pageSize);
    }

    public async Task<LoanReadRecord?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var loan = await _dbContext.Loans.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (loan is null)
        {
            return null;
        }

        var asset = await _dbContext.Assets.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == loan.AssetId, cancellationToken);
        var borrower = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == loan.BorrowerId, cancellationToken);

        return new LoanReadRecord(
            loan,
            asset?.AssetCode ?? string.Empty,
            asset?.Name ?? string.Empty,
            borrower?.FullName ?? string.Empty);
    }

    private IQueryable<Loan> BuildLoanQuery(LoanFilter filter)
    {
        var query = _dbContext.Loans.AsNoTracking().AsQueryable();
        if (filter.BorrowerId.HasValue)
        {
            query = query.Where(loan => loan.BorrowerId == filter.BorrowerId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(loan => loan.Status == filter.Status.Value);
        }

        if (filter.RequestedFrom.HasValue)
        {
            query = query.Where(loan => loan.RequestedAt >= filter.RequestedFrom.Value);
        }

        if (filter.RequestedTo.HasValue)
        {
            query = query.Where(loan => loan.RequestedAt <= filter.RequestedTo.Value);
        }

        if (filter.LoanFrom.HasValue)
        {
            query = query.Where(loan => loan.LoanDate >= filter.LoanFrom.Value);
        }

        if (filter.LoanTo.HasValue)
        {
            query = query.Where(loan => loan.LoanDate <= filter.LoanTo.Value);
        }

        if (filter.DueFrom.HasValue)
        {
            query = query.Where(loan => loan.DueDate >= filter.DueFrom.Value);
        }

        if (filter.DueTo.HasValue)
        {
            query = query.Where(loan => loan.DueDate <= filter.DueTo.Value);
        }

        if (filter.OverdueBefore.HasValue)
        {
            var cutoff = filter.OverdueBefore.Value;
            query = query.Where(loan =>
                loan.Status == Domain.Enums.LoanStatus.OnLoan
                && loan.DueDate < cutoff);
        }

        return query;
    }

    private IQueryable<LoanReadRecord> BuildReadQuery(IQueryable<Loan> loans) =>
        from loan in loans
        join asset in _dbContext.Assets.AsNoTracking()
            on loan.AssetId equals asset.Id into assets
        from asset in assets.DefaultIfEmpty()
        join borrower in _dbContext.Users.AsNoTracking()
            on loan.BorrowerId equals borrower.Id into borrowers
        from borrower in borrowers.DefaultIfEmpty()
        select new LoanReadRecord(
            loan,
            asset == null ? string.Empty : asset.AssetCode,
            asset == null ? string.Empty : asset.Name,
            borrower == null ? string.Empty : borrower.FullName);

    private static IOrderedQueryable<Loan> ApplyOrdering(
        IQueryable<Loan> query,
        string sortBy,
        bool descending) =>
        (sortBy.ToLowerInvariant(), descending) switch
        {
            ("id", false) => query.OrderBy(loan => loan.Id),
            ("id", true) => query.OrderByDescending(loan => loan.Id),
            ("requestedat", false) => query.OrderBy(loan => loan.RequestedAt),
            ("requestedat", true) => query.OrderByDescending(loan => loan.RequestedAt),
            ("loandate", false) => query.OrderBy(loan => loan.LoanDate),
            ("loandate", true) => query.OrderByDescending(loan => loan.LoanDate),
            ("duedate", false) => query.OrderBy(loan => loan.DueDate),
            ("duedate", true) => query.OrderByDescending(loan => loan.DueDate),
            ("returndate", false) => query.OrderBy(loan => loan.ReturnDate),
            ("returndate", true) => query.OrderByDescending(loan => loan.ReturnDate),
            ("status", false) => query.OrderBy(loan => loan.Status),
            ("status", true) => query.OrderByDescending(loan => loan.Status),
            ("createdat", false) => query.OrderBy(loan => loan.CreatedAt),
            ("createdat", true) => query.OrderByDescending(loan => loan.CreatedAt),
            _ => throw new InvalidOperationException("The requested sort field is not available.")
        };
}
