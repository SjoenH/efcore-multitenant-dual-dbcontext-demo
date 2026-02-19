using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services;

public interface ICustomersService
{
    Task<IReadOnlyList<CustomerResponse>> GetAll();
    Task<CustomerResponse?> GetById(Guid id);
    Task<CustomerResponse> Create(CreateCustomerRequest request);
    Task<bool> Update(Guid id, UpdateCustomerRequest request);
    Task<bool> Delete(Guid id);
}

public sealed class CustomersService : ICustomersService
{
    private readonly TenantDbContext _db;

    public CustomersService(TenantDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAll()
    {
        return await _db.Customers.AsNoTracking()
            .OrderBy(x => x.Name)
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

    public async Task<CustomerResponse> Create(CreateCustomerRequest request)
    {
        var entity = new Customer
        {
            Id = Guid.NewGuid(),
            BankId = _db.BankId,
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
