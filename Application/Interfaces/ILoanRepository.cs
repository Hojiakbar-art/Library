using Domain.Entities;
using Domain.Enums;
using Domain.Filters;

namespace Application.Interfaces;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetAllAsync(LoanFilter filter, CancellationToken cancellationToken = default);
    Task<int> CountAsync(LoanFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);
    Task UpdateAsync(Loan loan, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetActiveForReaderAsync(int readerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetDueAsync(DateTime now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Loan>> GetByReaderAsync(int readerId, LoanStatus? status, CancellationToken cancellationToken = default);
}
