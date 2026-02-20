using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using BankingApi.Models;

namespace BankingApi.Dtos;

public sealed class BankResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }

    public static Expression<Func<Bank, BankResponse>> Projection =>
        x => new BankResponse
        {
            Id = x.Id,
            Name = x.Name,
            Code = x.Code,
            CreatedAt = x.CreatedAt,
        };
}

public static class BankExtensions
{
    public static BankResponse ToResponse(this Bank x) =>
        new()
        {
            Id = x.Id,
            Name = x.Name,
            Code = x.Code,
            CreatedAt = x.CreatedAt,
        };

    public static void ApplyFields(this Bank entity, BankRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Code = request.Code.Trim().ToUpperInvariant();
    }
}

/// <summary>Shared fields for create and update bank requests.</summary>
public class BankRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
}

public sealed class CreateBankRequest : BankRequest { }

public sealed class UpdateBankRequest : BankRequest { }
