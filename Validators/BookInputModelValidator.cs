using BookLibrary.Data;
using BookLibrary.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Validators;

/// <summary>
/// Every validation rule for the book form, in one place.
///
/// Three kinds of rule are showcased here:
///
///   1. Built-in rules (NotEmpty, MaximumLength, InclusiveBetween, Matches).
///      These have client-side adapters, so AddFluentValidationClientsideAdapters()
///      in Program.cs turns them into data-val-* attributes on the rendered
///      <input> elements - jQuery unobtrusive validation then enforces them in
///      the browser instantly, with no round trip.
///
///   2. A custom synchronous rule (Must) - the ISBN checksum. FluentValidation
///      can't translate arbitrary C# into JavaScript, so this one is
///      server-side only. That's fine: the server is the source of truth
///      regardless, and the client-side rules are purely a UX nicety.
///
///   3. A custom asynchronous rule (MustAsync) - the "ISBN not already used"
///      check, which needs a database query. Also server-side only.
///
/// This validator takes AppDbContext via constructor injection, which works
/// because AddValidatorsFromAssemblyContaining registers validators as Scoped
/// by default - the same lifetime as the DbContext.
/// </summary>
public class BookInputModelValidator : AbstractValidator<BookInputModel>
{
    private readonly AppDbContext _db;

    public static readonly string[] Genres =
    {
        "Fiction", "Non-fiction", "Mystery", "Science fiction",
        "Fantasy", "Biography", "History", "Poetry", "Technical"
    };

    // A loose "looks like an ISBN" shape check: digits, optionally broken up by
    // hyphens or spaces, with an optional trailing X (the ISBN-10 check digit).
    // Deliberately permissive - this runs in the browser as a fast first pass,
    // and the checksum rule below does the real verification server-side.
    private const string IsbnShapePattern = @"^\d[\d\s-]*[\dX]$";

    public BookInputModelValidator(AppDbContext db)
    {
        _db = db;

        // Stop at the first failure within each field's chain. Two reasons:
        // a field reports one clear message at a time rather than a pile of
        // consequential ones, and - more importantly - later rules can assume
        // earlier ones passed. Without this, a null PublishedOn would fail
        // NotNull and then still run the "not in the future" rule, which
        // dereferences .Value and throws.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(b => b.Title)
            .NotEmpty().WithMessage("Enter the book's title.")
            .MaximumLength(200).WithMessage("Title can't be longer than 200 characters.");

        RuleFor(b => b.Author)
            .NotEmpty().WithMessage("Enter the author's name.")
            .MaximumLength(100).WithMessage("Author can't be longer than 100 characters.");

        // Thanks to the cascade mode above, a malformed ISBN reports "wrong
        // shape" without also running the checksum and firing a database query
        // against garbage.
        RuleFor(b => b.Isbn)
            .NotEmpty().WithMessage("Enter the ISBN.")
            .Matches(IsbnShapePattern)
                .WithMessage("An ISBN is digits only (an ISBN-10 may end in X). Hyphens and spaces are fine - they're ignored.")
            .Must(HaveValidChecksum)
                .WithMessage("That's not a valid ISBN - it must be 10 or 13 digits and the check digit must match.")
            .MustAsync(BeUniqueIsbn)
                .WithMessage("Another book in the library already has this ISBN.");

        RuleFor(b => b.Genre)
            .NotEmpty().WithMessage("Pick a genre.")
            .Must(g => Genres.Contains(g)).WithMessage("Pick a genre from the list.");

        RuleFor(b => b.PublishedOn)
            .NotNull().WithMessage("Enter the publication date.")
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Publication date can't be in the future.")
            .Must(d => d!.Value.Year >= 1450)
                .WithMessage("Publication date must be 1450 or later (printing wasn't a thing before that).");

        RuleFor(b => b.Price)
            .NotNull().WithMessage("Enter a price.")
            .InclusiveBetween(0m, 10_000m).WithMessage("Price must be between 0 and 10,000.");

        RuleFor(b => b.PageCount)
            .NotNull().WithMessage("Enter the page count.")
            .InclusiveBetween(1, 20_000).WithMessage("Page count must be between 1 and 20,000.");
    }

    /// <summary>
    /// Validates the ISBN's check digit - the arithmetic that makes a typo in
    /// any single digit detectable. This is the kind of rule that can only run
    /// server-side, since it's ordinary C# rather than something FluentValidation
    /// can translate into a jQuery validation rule.
    /// </summary>
    private static bool HaveValidChecksum(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn)) return false;

        var normalized = BookInputModel.NormalizeIsbn(isbn);

        return normalized.Length switch
        {
            10 => IsValidIsbn10(normalized),
            13 => IsValidIsbn13(normalized),
            _ => false
        };
    }

    private static bool IsValidIsbn10(string isbn)
    {
        var sum = 0;

        for (var i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn[i])) return false;
            sum += (isbn[i] - '0') * (10 - i);
        }

        // The final character is the check digit, where 'X' represents 10.
        var checkDigit = isbn[9] == 'X' ? 10 : isbn[9] - '0';
        if (isbn[9] != 'X' && !char.IsDigit(isbn[9])) return false;

        sum += checkDigit;
        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        var sum = 0;

        for (var i = 0; i < 13; i++)
        {
            if (!char.IsDigit(isbn[i])) return false;
            // Digits alternate weight 1, 3, 1, 3, ...
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return sum % 10 == 0;
    }

    /// <summary>
    /// Database-backed uniqueness check. The model overload gives access to the
    /// whole BookInputModel, so an edit can exclude the row it's editing -
    /// otherwise saving a book without changing its ISBN would report the book
    /// as a duplicate of itself.
    /// </summary>
    private async Task<bool> BeUniqueIsbn(BookInputModel model, string? isbn, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(isbn)) return true; // NotEmpty already reported this

        var normalized = BookInputModel.NormalizeIsbn(isbn);

        return !await _db.Books
            .AsNoTracking()
            .AnyAsync(b => b.Isbn == normalized && b.Id != model.Id, ct);
    }
}
