using BookLibrary.Data;
using BookLibrary.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookLibrary.Pages.Books;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IValidator<BookInputModel> _validator;

    public CreateModel(AppDbContext db, IValidator<BookInputModel> validator)
    {
        _db = db;
        _validator = validator;
    }

    [BindProperty]
    public BookInputModel Book { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // The validator is called explicitly rather than relying on
        // auto-validation, which lets it use async rules (the ISBN uniqueness
        // check queries the database).
        var result = await _validator.ValidateAsync(Book);

        if (!result.IsValid)
        {
            // Copies FluentValidation's failures into ModelState using the
            // "Book" prefix, so they line up with the asp-validation-for spans
            // bound to Book.Title, Book.Isbn, and so on.
            result.AddToModelState(ModelState, nameof(Book));
            return Page();
        }

        var book = BookInputModel.ToEntity(Book, new Book());

        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = $"Added \u201c{book.Title}\u201d to the library.";
        return RedirectToPage("./Index");
    }
}
