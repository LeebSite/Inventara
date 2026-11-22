using Inventara.Application.Common;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;

namespace Inventara.Application.Persistence;

public interface ILoanRepository : IEntityRepository<Loan>
{
    Task<bool> HasActiveLoanAsync(Guid assetId, CancellationToken cancellationToken);

    Task<PagedResult<LoanReadRecord>> GetPageAsync(
        LoanFilter filter,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken);

    Task<LoanReadRecord?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record LoanFilter(
    Guid? BorrowerId,
    LoanStatus? Status,
    DateTime? RequestedFrom,
    DateTime? RequestedTo,
    DateTime? LoanFrom,
    DateTime? LoanTo,
    DateTime? DueFrom,
    DateTime? DueTo,
    DateTime? OverdueBefore);

public sealed record LoanReadRecord(
    Loan Loan,
    string AssetCode,
    string AssetName,
    string BorrowerName);
