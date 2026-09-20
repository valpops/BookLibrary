using BookLibrary.Data;
using BookLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Pages.Books;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;

    public DeleteModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Book Book { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (book is null)
        {
            return NotFound();
        }

        Book = book;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == Book.Id);

        if (book is null)
        {
            // Already gone - someone else deleted it first. Nothing to undo,
            // so treat it as success rather than showing an error.
            TempData["StatusMessage"] = "That book was already removed from the library.";
            return RedirectToPage("./Index");
        }

        _db.Books.Remove(book);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = $"Deleted \u201c{book.Title}\u201d.";
        return RedirectToPage("./Index");
    }
}
