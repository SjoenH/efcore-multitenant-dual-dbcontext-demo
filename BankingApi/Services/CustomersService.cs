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
        return await _db
            .Customers.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(CustomerResponse.Projection)
            .ToListAsync();
    }

    public async Task<CustomerResponse?> GetById(Guid id)
    {
        return await _db
            .Customers.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(CustomerResponse.Projection)
            .SingleOrDefaultAsync();
    }

    public async Task<CustomerResponse> Create(CreateCustomerRequest request)
    {
        var entity = new Customer
        {
            Id = Guid.NewGuid(),
            BankId = _db.BankId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        entity.ApplyFields(request);

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToResponse();
    }

    public async Task<bool> Update(Guid id, UpdateCustomerRequest request)
    {
        var entity = await _db.Customers.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.ApplyFields(request);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(Guid id)
    {
        return await _db.DeleteByIdAsync(_db.Customers, id);
    }
}
