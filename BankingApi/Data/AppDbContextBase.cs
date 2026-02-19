using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Data;

public abstract class AppDbContextBase : DbContext
{
    protected AppDbContextBase(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Bank>(b =>
        {
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            b.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<Customer>(b =>
        {
            b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.HasIndex(x => new { x.BankId, x.Email });
            b.HasIndex(x => new { x.BankId, x.Name });

            b.HasMany(x => x.Accounts)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Account>(b =>
        {
            b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.Currency).HasDefaultValue("NOK");
            b.HasIndex(x => new { x.BankId, x.AccountNumber }).IsUnique();
            b.HasIndex(x => new { x.BankId, x.CustomerId });

            b.HasMany(x => x.Transactions)
                .WithOne(x => x.Account)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(b =>
        {
            b.HasIndex(x => new { x.BankId, x.AccountId, x.Timestamp });
            b.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(20);
        });
    }
}
