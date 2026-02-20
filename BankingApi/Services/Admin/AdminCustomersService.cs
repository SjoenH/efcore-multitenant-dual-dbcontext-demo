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
        return await _db
            .Customers.AsNoTracking()
            .OrderBy(x => x.BankId)
            .ThenBy(x => x.Name)
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

    public async Task<CustomerResponse> Create(CreateAdminCustomerRequest request)
    {
        var bankExists = await _db.Banks.AnyAsync(x => x.Id == request.BankId);
        if (!bankExists)
        {
            throw new InvalidOperationException("Bank not found");
        }

        var entity = new Customer
        {
            Id = Guid.NewGuid(),
            BankId = request.BankId,
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
