using System.ComponentModel.DataAnnotations;

namespace BookLibrary.Models;

/// <summary>
/// What the Create/Edit forms bind to. Kept separate from the Book entity so
/// the form can never over-post into fields it shouldn't touch, and so the
/// validator has a stable shape to target.
///
/// [Display] is used only for friendly labels/messages - the actual rules all
/// live in BookInputModelValidator.
/// </summary>
public class BookInputModel
{
    /// <summary>0 when creating; the existing book's id when editing. The
    /// "ISBN must be unique" rule uses this to exclude the row being edited
    /// from its own duplicate check.</summary>
    public int Id { get; set; }

    [Display(Name = "Title")]
    public string? Title { get; set; }

    [Display(Name = "Author")]
    public string? Author { get; set; }

    [Display(Name = "ISBN")]
    public string? Isbn { get; set; }

    [Display(Name = "Genre")]
    public string? Genre { get; set; }

    [Display(Name = "Published on")]
    [DataType(DataType.Date)]
    public DateOnly? PublishedOn { get; set; }

    [Display(Name = "Price")]
    public decimal? Price { get; set; }

    [Display(Name = "Pages")]
    public int? PageCount { get; set; }

    public static Book ToEntity(BookInputModel input, Book book)
    {
        book.Title = input.Title!.Trim();
        book.Author = input.Author!.Trim();
        book.Isbn = NormalizeIsbn(input.Isbn!);
        book.Genre = input.Genre!;
        book.PublishedOn = input.PublishedOn!.Value;
        book.Price = input.Price!.Value;
        book.PageCount = input.PageCount!.Value;
        return book;
    }

    public static BookInputModel FromEntity(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        Isbn = book.Isbn,
        Genre = book.Genre,
        PublishedOn = book.PublishedOn,
        Price = book.Price,
        PageCount = book.PageCount,
    };

    /// <summary>Strips the hyphens/spaces people naturally type into an ISBN.</summary>
    public static string NormalizeIsbn(string isbn) =>
        isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
}
