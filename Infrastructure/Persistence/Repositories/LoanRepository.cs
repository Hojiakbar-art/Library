using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly AppDbContext _context;

    public LoanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Loans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> GetAllAsync(LoanFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Loans.AsNoTracking().AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.ReaderId.HasValue)
        {
            query = query.Where(x => x.ReaderId == filter.ReaderId.Value);
        }

        query = query.OrderByDescending(x => x.BorrowedAt).ThenBy(x => x.Id);

        return await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(LoanFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Loans.AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.ReaderId.HasValue)
        {
            query = query.Where(x => x.ReaderId == filter.ReaderId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        await _context.Loans.AddAsync(loan, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        _context.Loans.Update(loan);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> GetActiveForReaderAsync(int readerId, CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .Where(x => x.ReaderId == readerId && (x.Status == LoanStatus.Active || x.Status == LoanStatus.Overdue))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> GetDueAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .Where(x => x.Status == LoanStatus.Active && x.DueAt <= now)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> GetByReaderAsync(int readerId, LoanStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Loans.Where(x => x.ReaderId == readerId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query.OrderByDescending(x => x.BorrowedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }
}
