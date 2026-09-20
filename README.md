# Book Library (.NET 8 / Razor Pages / EF Core / FluentValidation)

A small CRUD app for a book collection: list, search, create, edit, and delete
books, with all validation handled by FluentValidation.

## Run it

```bash
dotnet restore
dotnet run
```

Open the URL printed in the console. No database server needed - it uses
SQLite and creates `books.db` next to the app on first run, seeded with three
books.

To point it at SQL Server or Oracle instead, swap `UseSqlite` for
`UseSqlServer` / `UseOracle` in `Program.cs` and update the connection string
in `appsettings.json`.

## Project layout

| Path | What it's for |
|---|---|
| `Models/Book.cs` | The persisted entity. No validation attributes - rules live in the validator. |
| `Models/BookInputModel.cs` | What the forms bind to, plus mapping to/from the entity. |
| `Validators/BookInputModelValidator.cs` | Every validation rule, in one place. |
| `Data/AppDbContext.cs` | EF Core context, column config, unique ISBN index. |
| `Pages/Books/` | Index (list + search), Create, Edit, Delete. |
| `Pages/Books/_BookFormFields.cshtml` | Form fields shared by Create and Edit. |

## How FluentValidation is wired up

Two registrations in `Program.cs`:

```csharp
// Finds every AbstractValidator<T> in the assembly, registered Scoped so
// validators can inject scoped services like AppDbContext.
builder.Services.AddValidatorsFromAssemblyContaining<BookInputModelValidator>();

// Emits data-val-* attributes so jQuery unobtrusive validation can enforce
// the translatable rules in the browser.
builder.Services.AddFluentValidationClientsideAdapters();
```

Note there is deliberately **no** `AddFluentValidationAutoValidation()`. The
page handlers inject `IValidator<BookInputModel>` and call it explicitly:

```csharp
var result = await _validator.ValidateAsync(Book);

if (!result.IsValid)
{
    result.AddToModelState(ModelState, nameof(Book));
    return Page();
}
```

Two reasons for this:

1. **Async rules.** Auto-validation runs synchronously and throws if a
   validator contains `MustAsync` - and the ISBN uniqueness rule needs a
   database query.
2. It's the approach FluentValidation now recommends; auto-validation is
   maintained but no longer the default guidance.

## The validation rules

| Field | Rules |
|---|---|
| Title | Required, max 200 chars |
| Author | Required, max 100 chars |
| ISBN | Required, digit/hyphen shape, **valid check digit**, **not already used by another book** |
| Genre | Required, must be one of the known genres |
| Published on | Required, not in the future, year >= 1450 |
| Price | Required, between 0 and 10,000 |
| Pages | Required, between 1 and 20,000 |

Three kinds of rule are showcased deliberately:

- **Built-in rules** (`NotEmpty`, `MaximumLength`, `Matches`,
  `InclusiveBetween`) have client-side equivalents, so they fire instantly in
  the browser *and* again on the server.
- **A custom sync rule** (`Must`) - the ISBN check-digit arithmetic. Ordinary
  C#, so it can't be translated to JavaScript: server-side only.
- **A custom async rule** (`MustAsync`) - the "no other book has this ISBN"
  check, which queries the database. Server-side only, and it excludes the row
  being edited so a book is never reported as a duplicate of itself.

Client-side validation is only ever a convenience here. Every rule runs on the
server on every POST, regardless of what the browser did.

## Things worth trying

- Enter `1234567890` as an ISBN - it passes the shape check in the browser,
  then fails the checksum on the server.
- Try to add a second book with ISBN `9780441013593` (Dune's, already seeded) -
  the async uniqueness rule catches it.
- Edit Dune without changing its ISBN - it saves fine, because the uniqueness
  rule excludes the book being edited.
- Set a publication date in the future - rejected server-side.
- Disable JavaScript and submit an empty form - every rule still fires.

## Defence in depth on the ISBN

The `MustAsync` rule gives a friendly error message, but the real guarantee is
the unique index on `Books.Isbn` in `AppDbContext`. Two simultaneous requests
can both pass validation in the same instant; only one can win the insert. The
validator makes the common case pleasant, the index makes the race impossible
to lose silently.
