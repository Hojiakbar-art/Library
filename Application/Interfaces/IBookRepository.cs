using Domain.Entities;
using Domain.Filters;

namespace Application.Interfaces;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Book>> GetAllAsync(BookFilter filter, CancellationToken cancellationToken = default);
    Task<int> CountAsync(BookFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(Book book, CancellationToken cancellationToken = default);
    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);
    Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default);
}
