using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services.Admin;

public interface IAdminCustomersService
{
    Task<IReadOnlyList<CustomerResponse>> GetAll();
    Task<CustomerResponse?> GetById(Guid id);
    Task<CustomerResponse> Create(CreateAdminCustomerRequest request);
    Task<bool> Update(Guid id, UpdateCustomerRequest request);
    Task<bool> Delete(Guid id);
}

public sealed class AdminCustomersService : IAdminCustomersService
{
    private readonly AdminDbContext _db;

    public AdminCustomersService(AdminDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAll()
    {
        return await _db.Customers.AsNoTracking()
            .OrderBy(x => x.BankId)
            .ThenBy(x => x.Name)
            .Select(x => new CustomerResponse
            {
                Id = x.Id,
                BankId = x.BankId,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CustomerResponse?> GetById(Guid id)
    {
        return await _db.Customers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CustomerResponse
            {
                Id = x.Id,
                BankId = x.BankId,
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                CreatedAt = x.CreatedAt
            })
            .SingleOrDefaultAsync();
    }

    public async Task<CustomerResponse> Create(CreateAdminCustomerRequest request)
    {
        if (request.BankId == Guid.Empty)
        {
            throw new InvalidOperationException("BankId is required");
        }

        var bankExists = await _db.Banks.AnyAsync(x => x.Id == request.BankId);
        if (!bankExists)
        {
            throw new InvalidOperationException("Bank not found");
        }

        var entity = new Customer
        {
            Id = Guid.NewGuid(),
            BankId = request.BankId,
            Name = request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync();

        return new CustomerResponse
        {
            Id = entity.Id,
            BankId = entity.BankId,
            Name = entity.Name,
            Email = entity.Email,
            Phone = entity.Phone,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> Update(Guid id, UpdateCustomerRequest request)
    {
        var entity = await _db.Customers.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.Name = request.Name.Trim();
        entity.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        entity.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(Guid id)
    {
        var entity = await _db.Customers.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return false;
        }

        _db.Customers.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
