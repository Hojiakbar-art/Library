using Application.ApiResult;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Services;

public class LoanOverdueService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoanOverdueService(ILoanRepository loanRepository, IEmailService emailService, IDateTimeProvider dateTimeProvider)
    {
        _loanRepository = loanRepository;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<int>> MarkOverdueAsync(CancellationToken cancellationToken = default)
    {
        var overdueLoans = await _loanRepository.GetDueAsync(_dateTimeProvider.UtcNow, cancellationToken);
        var count = 0;

        foreach (var loan in overdueLoans)
        {
            if (loan.Status != LoanStatus.Active)
            {
                continue;
            }

            loan.Status = LoanStatus.Overdue;
            await _loanRepository.UpdateAsync(loan, cancellationToken);
            count++;

            try
            {
                await _emailService.SendOverdueAsync("reader@example.com", "Book", loan.DueAt, 0.50m, cancellationToken);
            }
            catch
            {
            }
        }

        return Result<int>.Success(count);
    }
}
