using Application.Identity;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Author).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Isbn).IsRequired().HasMaxLength(30);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.Isbn).IsUnique();
            entity.Property(x => x.TotalCopies).IsRequired();
            entity.Property(x => x.AvailableCopies).IsRequired();
            entity.Property(x => x.CreatedByUserId).IsRequired();
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.ToTable("Loans");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BorrowedAt).IsRequired();
            entity.Property(x => x.DueAt).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.Fine).HasColumnType("numeric(18,2)").HasDefaultValue(0m);
            entity.HasOne<Book>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.FirstName).IsRequired().HasMaxLength(250);
            entity.Property(x => x.LastName).IsRequired().HasMaxLength(250);
            entity.Property(x => x.BirthDate).HasColumnType("date");
        });
    }
}
