namespace BookLibrary.Models;

/// <summary>
/// The persisted entity. Deliberately free of validation attributes - all
/// validation lives in Validators/BookInputModelValidator.cs so there's a
/// single place to look, rather than rules split between DataAnnotations
/// here and FluentValidation there.
/// </summary>
public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string Author { get; set; } = "";

    public string Isbn { get; set; } = "";

    public string Genre { get; set; } = "";

    public DateOnly PublishedOn { get; set; }

    public decimal Price { get; set; }

    public int PageCount { get; set; }
}
