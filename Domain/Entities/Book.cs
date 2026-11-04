namespace Domain.Entities;

public class Book
{
    public int Id{get;set;}
    public string Title{get;set;}=null!;
    public string Author{get;set;}=null!;
    public string Isbn{get;set;}=null!;
    public int PublishedYear{get;set;}
    public int TotalCopies{get;set;}
    public int AvailableCopies{get;set;}
    public DateTime CreatedAt{get;set;}
    public int CreatedByUserId{get;set;}
}
