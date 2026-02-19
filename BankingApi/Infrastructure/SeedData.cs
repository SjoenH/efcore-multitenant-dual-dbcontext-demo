using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure;

public static class SeedData
{
    public const string AdminEmail = "admin@demo.com";
    public const string StaffBankAEmail = "staff.norge@demo.com";
    public const string StaffBankBEmail = "staff.svensk@demo.com";
    public const string CustomerAEmail = "customer.ola@demo.com";
    public const string CustomerBEmail = "customer.anna@demo.com";

    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BankingApi.Data.AdminDbContext>();

        // If we already have banks, assume seeded.
        if (await db.Banks.AnyAsync())
        {
            return;
        }

        var bankA = new Bank
        {
            Id = Guid.NewGuid(),
            Name = "Norge Bank",
            Code = "NO-001",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var bankB = new Bank
        {
            Id = Guid.NewGuid(),
            Name = "Svensk Bank",
            Code = "SE-001",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var customerA = new Customer
        {
            Id = Guid.NewGuid(),
            BankId = bankA.Id,
            Name = "Ola Nordmann",
            Email = CustomerAEmail,
            Phone = "+47 999 00 000",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var customerB = new Customer
        {
            Id = Guid.NewGuid(),
            BankId = bankB.Id,
            Name = "Anna Andersson",
            Email = CustomerBEmail,
            Phone = "+46 70 000 00 00",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var accountA = new Account
        {
            Id = Guid.NewGuid(),
            BankId = bankA.Id,
            CustomerId = customerA.Id,
            AccountNumber = $"{bankA.Code}-{Guid.NewGuid():N}".ToUpperInvariant(),
            Balance = 10000m,
            Currency = "NOK",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var accountB = new Account
        {
            Id = Guid.NewGuid(),
            BankId = bankB.Id,
            CustomerId = customerB.Id,
            AccountNumber = $"{bankB.Code}-{Guid.NewGuid():N}".ToUpperInvariant(),
            Balance = 15000m,
            Currency = "NOK",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var txA1 = new Transaction
        {
            Id = Guid.NewGuid(),
            BankId = bankA.Id,
            AccountId = accountA.Id,
            Amount = 10000m,
            Type = TransactionType.Credit,
            Description = "Initial deposit",
            Timestamp = DateTimeOffset.UtcNow.AddDays(-10)
        };

        var txB1 = new Transaction
        {
            Id = Guid.NewGuid(),
            BankId = bankB.Id,
            AccountId = accountB.Id,
            Amount = 15000m,
            Type = TransactionType.Credit,
            Description = "Initial deposit",
            Timestamp = DateTimeOffset.UtcNow.AddDays(-10)
        };

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = AdminEmail,
            Role = Role.Admin,
            BankId = null,
            CustomerId = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var staffA = new User
        {
            Id = Guid.NewGuid(),
            Email = StaffBankAEmail,
            Role = Role.Staff,
            BankId = bankA.Id,
            CustomerId = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var staffB = new User
        {
            Id = Guid.NewGuid(),
            Email = StaffBankBEmail,
            Role = Role.Staff,
            BankId = bankB.Id,
            CustomerId = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var userCustomerA = new User
        {
            Id = Guid.NewGuid(),
            Email = CustomerAEmail,
            Role = Role.Customer,
            BankId = bankA.Id,
            CustomerId = customerA.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var userCustomerB = new User
        {
            Id = Guid.NewGuid(),
            Email = CustomerBEmail,
            Role = Role.Customer,
            BankId = bankB.Id,
            CustomerId = customerB.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Banks.AddRange(bankA, bankB);
        db.Customers.AddRange(customerA, customerB);
        db.Accounts.AddRange(accountA, accountB);
        db.Transactions.AddRange(txA1, txB1);
        db.Users.AddRange(admin, staffA, staffB, userCustomerA, userCustomerB);

        await db.SaveChangesAsync();
    }
}
