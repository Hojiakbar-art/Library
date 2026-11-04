namespace Application.DTOs;

public class BookRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public int TotalCopies { get; set; }
}

public class LoanRequestDto
{
    public int BookId { get; set; }
}

public class LoanReturnRequestDto
{
    public int LoanId { get; set; }
}
