namespace Application.Interfaces;

public interface IEmailService
{
    Task SendBookBorrowedAsync(string email, string bookTitle, DateTime dueAt, CancellationToken cancellationToken = default);
    Task SendBookReturnedAsync(string email, string bookTitle, decimal fine, CancellationToken cancellationToken = default);
    Task SendOverdueAsync(string email, string bookTitle, DateTime dueAt, decimal finePerDay, CancellationToken cancellationToken = default);
}
