using Inventara.Application.Common;

namespace Inventara.Application.Loans;

public interface ILoanService
{
    Task<LoanResponse> CreateAsync(
        Guid borrowerId,
        CreateLoanRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<LoanResponse>> GetMyPageAsync(
        Guid borrowerId,
        LoanListQuery query,
        CancellationToken cancellationToken);

    Task<PagedResponse<LoanResponse>> GetPageAsync(
        LoanListQuery query,
        CancellationToken cancellationToken);

    Task<LoanResponse> GetByIdAsync(
        Guid id,
        Guid requesterId,
        bool canViewAll,
        CancellationToken cancellationToken);

    Task<LoanResponse> ApproveAsync(
        Guid id,
        Guid approverId,
        CancellationToken cancellationToken);

    Task<LoanResponse> RejectAsync(
        Guid id,
        Guid approverId,
        RejectLoanRequest request,
        CancellationToken cancellationToken);

    Task<LoanResponse> ReturnAsync(
        Guid id,
        Guid reporterId,
        ReturnLoanRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<LoanResponse>> GetOverduePageAsync(
        LoanListQuery query,
        CancellationToken cancellationToken);
}
