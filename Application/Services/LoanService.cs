using Application.ApiResult;
using Application.DTOs;
using Application.Interfaces;
using Application.Utils;
using Domain.Entities;
using Domain.Enums;
using Domain.Filters;

namespace Application.Services;

public class LoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmailService _emailService;
    private readonly Application.Settings.LoanSettings _loanSettings;

    public LoanService(
        ILoanRepository loanRepository,
        IBookRepository bookRepository,
        IDateTimeProvider dateTimeProvider,
        IEmailService emailService,
        Application.Settings.LoanSettings loanSettings)
    {
        _loanRepository = loanRepository;
        _bookRepository = bookRepository;
        _dateTimeProvider = dateTimeProvider;
        _emailService = emailService;
        _loanSettings = loanSettings;
    }

    public async Task<Result<LoanDto>> BorrowAsync(int readerId, int bookId, CancellationToken cancellationToken = default)
    {
        var book = await _bookRepository.GetByIdAsync(bookId, cancellationToken);
        if (book == null)
        {
            return Result<LoanDto>.Fail("Book not found.", ErrorType.NotFound);
        }

        var activeReaderLoans = await _loanRepository.GetByReaderAsync(readerId, null, cancellationToken);
        if (activeReaderLoans.Any(x => x.Status == LoanStatus.Overdue || x.Status == LoanStatus.Active && x.DueAt <= _dateTimeProvider.UtcNow))
        {
            return Result<LoanDto>.Fail("Reader has an overdue loan.", ErrorType.Conflict);
        }

        if (activeReaderLoans.Count(x => x.Status == LoanStatus.Active || x.Status == LoanStatus.Overdue) >= _loanSettings.MaxActiveLoans)
        {
            return Result<LoanDto>.Fail("Maximum active loans reached.", ErrorType.Conflict);
        }

        if (activeReaderLoans.Any(x => x.BookId == bookId && (x.Status == LoanStatus.Active || x.Status == LoanStatus.Overdue)))
        {
            return Result<LoanDto>.Fail("Reader already has this book.", ErrorType.Conflict);
        }

        if (book.AvailableCopies <= 0)
        {
            return Result<LoanDto>.Fail("No copies available.", ErrorType.Conflict);
        }

        var now = _dateTimeProvider.UtcNow;
        var loan = new Loan
        {
            BookId = bookId,
            ReaderId = readerId,
            BorrowedAt = now,
            DueAt = now.AddDays(_loanSettings.LoanDays),
            Status = LoanStatus.Active,
            Fine = 0m
        };

        book.AvailableCopies -= 1;
        await _loanRepository.AddAsync(loan, cancellationToken);
        await _bookRepository.UpdateAsync(book, cancellationToken);

        try
        {
            await _emailService.SendBookBorrowedAsync("reader@example.com", book.Title, loan.DueAt, cancellationToken);
        }
        catch
        {
        }

        return Result<LoanDto>.Success(MapToDto(loan, book.Title));
    }

    public async Task<Result<LoanDto>> ReturnAsync(int loanId, int currentUserId, bool isLibrarian, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdAsync(loanId, cancellationToken);
        if (loan == null)
        {
            return Result<LoanDto>.Fail("Loan not found.", ErrorType.NotFound);
        }

        if (loan.ReaderId != currentUserId && !isLibrarian && !isAdmin)
        {
            return Result<LoanDto>.Fail("You are not allowed to return this loan.", ErrorType.Forbidden);
        }

        if (loan.Status == LoanStatus.Returned)
        {
            return Result<LoanDto>.Fail("Loan is already returned.", ErrorType.Conflict);
        }

        var book = await _bookRepository.GetByIdAsync(loan.BookId, cancellationToken);
        if (book == null)
        {
            return Result<LoanDto>.Fail("Book not found.", ErrorType.NotFound);
        }

        var now = _dateTimeProvider.UtcNow;
        var fine = FineCalculator.Calculate(loan.DueAt, now, _loanSettings.FinePerDay);
        loan.ReturnedAt = now;
        loan.Status = LoanStatus.Returned;
        loan.Fine = fine;
        book.AvailableCopies += 1;

        await _loanRepository.UpdateAsync(loan, cancellationToken);
        await _bookRepository.UpdateAsync(book, cancellationToken);

        try
        {
            await _emailService.SendBookReturnedAsync("reader@example.com", book.Title, loan.Fine, cancellationToken);
        }
        catch
        {
        }

        return Result<LoanDto>.Success(MapToDto(loan, book.Title));
    }

    public async Task<Result<PagedResult<LoanDto>>> GetMyLoansAsync(int readerId, LoanStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (status.HasValue && !Enum.IsDefined(typeof(LoanStatus), status.Value))
        {
            return Result<PagedResult<LoanDto>>.Fail("Unknown loan status.", ErrorType.Validation);
        }

        var loans = await _loanRepository.GetByReaderAsync(readerId, status, cancellationToken);
        var items = loans
            .OrderByDescending(x => x.BorrowedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LoanDto
            {
                Id = x.Id,
                BookId = x.BookId,
                BookTitle = "",
                ReaderId = x.ReaderId,
                BorrowedAt = x.BorrowedAt,
                DueAt = x.DueAt,
                ReturnedAt = x.ReturnedAt,
                Status = x.Status,
                Fine = x.Fine
            })
            .ToList();

        return Result<PagedResult<LoanDto>>.Success(new PagedResult<LoanDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = loans.Count,
            TotalPages = (int)Math.Ceiling(loans.Count / (double)pageSize),
            HasPrevious = page > 1,
            HasNext = page < Math.Ceiling(loans.Count / (double)pageSize)
        });
    }

    public async Task<Result<PagedResult<LoanDto>>> GetAllAsync(LoanFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.Status.HasValue && !Enum.IsDefined(typeof(LoanStatus), filter.Status.Value))
        {
            return Result<PagedResult<LoanDto>>.Fail("Unknown loan status.", ErrorType.Validation);
        }

        var loans = await _loanRepository.GetAllAsync(filter, cancellationToken);
        var items = loans
            .Select(x => MapToDto(x, ""))
            .ToList();

        return Result<PagedResult<LoanDto>>.Success(new PagedResult<LoanDto>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = items.Count,
            TotalPages = (int)Math.Ceiling(items.Count / (double)filter.PageSize),
            HasPrevious = filter.Page > 1,
            HasNext = filter.Page < Math.Ceiling(items.Count / (double)filter.PageSize)
        });
    }

    private static LoanDto MapToDto(Loan loan, string bookTitle) => new()
    {
        Id = loan.Id,
        BookId = loan.BookId,
        BookTitle = bookTitle,
        ReaderId = loan.ReaderId,
        BorrowedAt = loan.BorrowedAt,
        DueAt = loan.DueAt,
        ReturnedAt = loan.ReturnedAt,
        Status = loan.Status,
        Fine = loan.Fine
    };
}
