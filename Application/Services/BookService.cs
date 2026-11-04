using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace Application.Services;

public class BookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IMemoryCache _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BookService(IBookRepository bookRepository, IMemoryCache cache, IDateTimeProvider dateTimeProvider)
    {
        _bookRepository = bookRepository;
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<BookDto>> CreateAsync(BookRequestDto request, int createdByUserId, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return Result<BookDto>.Fail("Request is required.", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 2 || request.Title.Trim().Length > 200)
        {
            return Result<BookDto>.Fail("Title is invalid.", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(request.Author) || request.Author.Trim().Length < 2 || request.Author.Trim().Length > 150)
        {
            return Result<BookDto>.Fail("Author is invalid.", ErrorType.Validation);
        }

        var isbn = request.Isbn?.Replace("-", "").Replace(" ", "") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(isbn) || (isbn.Length != 10 && isbn.Length != 13) || !isbn.All(char.IsDigit))
        {
            return Result<BookDto>.Fail("ISBN is invalid.", ErrorType.Validation);
        }

        if (request.PublishedYear < 1450 || request.PublishedYear > _dateTimeProvider.UtcNow.Year)
        {
            return Result<BookDto>.Fail("Published year is invalid.", ErrorType.Validation);
        }

        if (request.TotalCopies < 1 || request.TotalCopies > 1000)
        {
            return Result<BookDto>.Fail("Total copies is invalid.", ErrorType.Validation);
        }

        var existing = await _bookRepository.GetByIsbnAsync(isbn, cancellationToken);
        if (existing != null)
        {
            return Result<BookDto>.Fail("ISBN already exists.", ErrorType.Conflict);
        }

        var book = new Book
        {
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Isbn = isbn,
            PublishedYear = request.PublishedYear,
            TotalCopies = request.TotalCopies,
            AvailableCopies = request.TotalCopies,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _bookRepository.AddAsync(book, cancellationToken);
        InvalidateBookCache();

        return Result<BookDto>.Success(MapToDto(book));
    }

    public async Task<Result<BookDto>> UpdateAsync(int id, BookRequestDto request, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (book == null)
        {
            return Result<BookDto>.Fail("Book not found.", ErrorType.NotFound);
        }

        if (book.CreatedByUserId != currentUserId && !isAdmin)
        {
            return Result<BookDto>.Fail("You are not allowed to edit this book.", ErrorType.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 2 || request.Title.Trim().Length > 200)
        {
            return Result<BookDto>.Fail("Title is invalid.", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(request.Author) || request.Author.Trim().Length < 2 || request.Author.Trim().Length > 150)
        {
            return Result<BookDto>.Fail("Author is invalid.", ErrorType.Validation);
        }

        var isbn = request.Isbn?.Replace("-", "").Replace(" ", "") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(isbn) || (isbn.Length != 10 && isbn.Length != 13) || !isbn.All(char.IsDigit))
        {
            return Result<BookDto>.Fail("ISBN is invalid.", ErrorType.Validation);
        }

        if (request.PublishedYear < 1450 || request.PublishedYear > _dateTimeProvider.UtcNow.Year)
        {
            return Result<BookDto>.Fail("Published year is invalid.", ErrorType.Validation);
        }

        if (request.TotalCopies < 1 || request.TotalCopies > 1000)
        {
            return Result<BookDto>.Fail("Total copies is invalid.", ErrorType.Validation);
        }

        if (request.TotalCopies < book.TotalCopies - (book.TotalCopies - book.AvailableCopies))
        {
            return Result<BookDto>.Fail("Total copies cannot be less than the number of borrowed copies.", ErrorType.Validation);
        }

        var existing = await _bookRepository.GetByIsbnAsync(isbn, cancellationToken);
        if (existing != null && existing.Id != id)
        {
            return Result<BookDto>.Fail("ISBN already exists.", ErrorType.Conflict);
        }

        var borrowedCopies = book.TotalCopies - book.AvailableCopies;
        var newAvailable = request.TotalCopies - borrowedCopies;
        book.Title = request.Title.Trim();
        book.Author = request.Author.Trim();
        book.Isbn = isbn;
        book.PublishedYear = request.PublishedYear;
        book.TotalCopies = request.TotalCopies;
        book.AvailableCopies = Math.Max(0, newAvailable);

        await _bookRepository.UpdateAsync(book, cancellationToken);
        InvalidateBookCache();

        return Result<BookDto>.Success(MapToDto(book));
    }

    public async Task<Result<PagedResult<BookDto>>> GetBooksAsync(BookFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter == null)
        {
            return Result<PagedResult<BookDto>>.Fail("Filter is required.", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(filter.SortBy))
        {
            filter.SortBy = "title";
        }

        var allowed = new[] { "title", "author", "year" };
        if (!allowed.Contains(filter.SortBy.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return Result<PagedResult<BookDto>>.Fail("Unknown sort field.", ErrorType.Validation);
        }

        if (filter.MinYear.HasValue && filter.MaxYear.HasValue && filter.MinYear.Value > filter.MaxYear.Value)
        {
            return Result<PagedResult<BookDto>>.Fail("MinYear cannot be greater than MaxYear.", ErrorType.Validation);
        }

        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (filter.PageSize > 100) filter.PageSize = 100;

        var key = $"books:{filter.Search}:{filter.OnlyAvailable}:{filter.MinYear}:{filter.MaxYear}:{filter.SortBy}:{filter.Desc}:{filter.Page}:{filter.PageSize}";
        if (!_cache.TryGetValue(key, out PagedResult<BookDto>? cached))
        {
            var books = await _bookRepository.GetAllAsync(filter, cancellationToken);
            var total = await _bookRepository.CountAsync(filter, cancellationToken);
            var items = books.Select(MapToDto).ToList();
            var result = new PagedResult<BookDto>
            {
                Items = items,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
                HasPrevious = filter.Page > 1,
                HasNext = filter.Page < Math.Ceiling(total / (double)filter.PageSize)
            };

            _cache.Set(key, result, TimeSpan.FromMinutes(5));
            return Result<PagedResult<BookDto>>.Success(result);
        }

        return Result<PagedResult<BookDto>>.Success(cached!);
    }

    public async Task<Result<BookDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var key = $"book:{id}";
        if (_cache.TryGetValue(key, out BookDto? cached))
        {
            return Result<BookDto>.Success(cached!);
        }

        var book = await _bookRepository.GetByIdAsync(id, cancellationToken);
        if (book == null)
        {
            return Result<BookDto>.Fail("Book not found.", ErrorType.NotFound);
        }

        var dto = MapToDto(book);
        _cache.Set(key, dto, TimeSpan.FromMinutes(5));
        return Result<BookDto>.Success(dto);
    }

    private static BookDto MapToDto(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        Isbn = book.Isbn,
        PublishedYear = book.PublishedYear,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        CreatedAt = book.CreatedAt
    };

    private void InvalidateBookCache()
    {
        _cache.Remove("books");
        _cache.Remove("book");
    }
}
