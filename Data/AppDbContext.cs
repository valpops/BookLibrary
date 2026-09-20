using BookLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(b => b.Title).HasMaxLength(200).IsRequired();
            entity.Property(b => b.Author).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Isbn).HasMaxLength(20).IsRequired();
            entity.Property(b => b.Genre).HasMaxLength(50).IsRequired();

            // SQLite stores decimal as TEXT by default and warns about it;
            // being explicit keeps the model unambiguous.
            entity.Property(b => b.Price).HasColumnType("decimal(18,2)");

            // The validator's MustAsync uniqueness rule gives a friendly error
            // message; this index is the actual guarantee. Two requests can
            // both pass validation at the same instant and only one can win -
            // the database is what makes that race impossible to lose silently.
            entity.HasIndex(b => b.Isbn).IsUnique();
        });
    }
}
