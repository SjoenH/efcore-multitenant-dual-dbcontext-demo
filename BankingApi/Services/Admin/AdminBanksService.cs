using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services.Admin;

public interface IAdminBanksService
{
    Task<IReadOnlyList<BankResponse>> GetAll();
    Task<BankResponse?> GetById(Guid id);
    Task<BankResponse> Create(CreateBankRequest request);
    Task<bool> Update(Guid id, UpdateBankRequest request);
    Task<bool> Delete(Guid id);
}

public sealed class AdminBanksService : IAdminBanksService
{
    private readonly AdminDbContext _db;

    public AdminBanksService(AdminDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BankResponse>> GetAll()
    {
        return await _db.Banks.AsNoTracking().OrderBy(x => x.Code).Select(BankResponse.Projection).ToListAsync();
    }

    public async Task<BankResponse?> GetById(Guid id)
    {
        return await _db
            .Banks.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(BankResponse.Projection)
            .SingleOrDefaultAsync();
    }

    public async Task<BankResponse> Create(CreateBankRequest request)
    {
        var entity = new Bank
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Banks.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToResponse();
    }

    public async Task<bool> Update(Guid id, UpdateBankRequest request)
    {
        var entity = await _db.Banks.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return false;
        }

        entity.Name = request.Name.Trim();
        entity.Code = request.Code.Trim().ToUpperInvariant();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(Guid id)
    {
        return await _db.DeleteByIdAsync(_db.Banks, id);
    }
}
