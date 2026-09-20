using BookLibrary.Data;
using BookLibrary.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Pages.Books;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public IList<Book> Books { get; set; } = new List<Book>();

    public string? Search { get; set; }

    public async Task OnGetAsync(string? search)
    {
        Search = search;

        IQueryable<Book> query = _db.Books.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b =>
                b.Title.Contains(term) ||
                b.Author.Contains(term) ||
                b.Isbn.Contains(term));
        }

        Books = await query
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Id) // tiebreaker keeps ordering stable for duplicate titles
            .ToListAsync();
    }
}
