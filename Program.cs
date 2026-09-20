using BookLibrary.Data;
using BookLibrary.Models;
using BookLibrary.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// --- FluentValidation wiring -------------------------------------------------
//
// Registers every AbstractValidator<T> in this assembly (here: just
// BookInputModelValidator) with a Scoped lifetime, so validators can inject
// scoped services like AppDbContext.
builder.Services.AddValidatorsFromAssemblyContaining<BookInputModelValidator>();

// Emits data-val-* attributes for the rules that have client-side equivalents
// (NotEmpty, MaximumLength, Matches, InclusiveBetween...), so jQuery
// unobtrusive validation can enforce them in the browser with no round trip.
//
// Note there's deliberately no AddFluentValidationAutoValidation() here. The
// page handlers call IValidator<BookInputModel> explicitly instead, because
// auto-validation runs synchronously and would throw on this validator's
// async MustAsync rule. Calling the validator by hand is also the approach
// FluentValidation now recommends over auto-validation.
builder.Services.AddFluentValidationClientsideAdapters();
// -----------------------------------------------------------------------------

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedBooks(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Books"));

app.Run();

static void SeedBooks(AppDbContext db)
{
    if (db.Books.Any()) return;

    db.Books.AddRange(
        new Book
        {
            Title = "The Pragmatic Programmer",
            Author = "Andrew Hunt, David Thomas",
            Isbn = "9780135957059",
            Genre = "Technical",
            PublishedOn = new DateOnly(2019, 9, 13),
            Price = 44.99m,
            PageCount = 352
        },
        new Book
        {
            Title = "Dune",
            Author = "Frank Herbert",
            Isbn = "9780441013593",
            Genre = "Science fiction",
            PublishedOn = new DateOnly(1965, 8, 1),
            Price = 12.50m,
            PageCount = 688
        },
        new Book
        {
            Title = "The Name of the Wind",
            Author = "Patrick Rothfuss",
            Isbn = "9780756404741",
            Genre = "Fantasy",
            PublishedOn = new DateOnly(2007, 3, 27),
            Price = 18.00m,
            PageCount = 662
        });

    db.SaveChanges();
}
