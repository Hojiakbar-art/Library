using Application.Interfaces;
using Domain.Entities;
using Domain.Filters;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class BookRepository : IBookRepository
{
    private readonly AppDbContext _context;

    public BookRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Books.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(BookFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Books.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Title.Contains(search) || x.Author.Contains(search));
        }

        if (filter.OnlyAvailable)
        {
            query = query.Where(x => x.AvailableCopies > 0);
        }

        if (filter.MinYear.HasValue)
        {
            query = query.Where(x => x.PublishedYear >= filter.MinYear.Value);
        }

        if (filter.MaxYear.HasValue)
        {
            query = query.Where(x => x.PublishedYear <= filter.MaxYear.Value);
        }

        query = filter.SortBy.Trim() switch
        {
            "title" => filter.Desc ? query.OrderByDescending(x => x.Title).ThenBy(x => x.Id) : query.OrderBy(x => x.Title).ThenBy(x => x.Id),
            "author" => filter.Desc ? query.OrderByDescending(x => x.Author).ThenBy(x => x.Id) : query.OrderBy(x => x.Author).ThenBy(x => x.Id),
            _ => filter.Desc ? query.OrderByDescending(x => x.PublishedYear).ThenBy(x => x.Id) : query.OrderBy(x => x.PublishedYear).ThenBy(x => x.Id)
        };

        return await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(BookFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Title.Contains(search) || x.Author.Contains(search));
        }

        if (filter.OnlyAvailable)
        {
            query = query.Where(x => x.AvailableCopies > 0);
        }

        if (filter.MinYear.HasValue)
        {
            query = query.Where(x => x.PublishedYear >= filter.MinYear.Value);
        }

        if (filter.MaxYear.HasValue)
        {
            query = query.Where(x => x.PublishedYear <= filter.MaxYear.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        await _context.Books.AddAsync(book, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        _context.Books.Update(book);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        return await _context.Books.AsNoTracking().FirstOrDefaultAsync(x => x.Isbn == isbn, cancellationToken);
    }
}
