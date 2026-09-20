using BookLibrary.Data;
using BookLibrary.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Pages.Books;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IValidator<BookInputModel> _validator;

    public EditModel(AppDbContext db, IValidator<BookInputModel> validator)
    {
        _db = db;
        _validator = validator;
    }

    [BindProperty]
    public BookInputModel Book { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (book is null)
        {
            return NotFound();
        }

        Book = BookInputModel.FromEntity(book);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _validator.ValidateAsync(Book);

        if (!result.IsValid)
        {
            result.AddToModelState(ModelState, nameof(Book));
            return Page();
        }

        // Load the tracked entity and copy the validated values onto it, rather
        // than attaching the input model - this way only the fields the form
        // actually owns are ever written.
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == Book.Id);
        if (book is null)
        {
            // Someone deleted it between the GET and the POST.
            ModelState.AddModelError(string.Empty,
                "This book was deleted while you were editing it. Your changes weren't saved.");
            return Page();
        }

        BookInputModel.ToEntity(Book, book);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = $"Saved changes to \u201c{book.Title}\u201d.";
        return RedirectToPage("./Index");
    }
}
