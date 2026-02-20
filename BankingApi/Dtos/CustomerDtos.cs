using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using BankingApi.Models;

namespace BankingApi.Dtos;

public sealed class CustomerResponse
{
    public Guid Id { get; init; }
    public Guid BankId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static Expression<Func<Customer, CustomerResponse>> Projection =>
        x => new CustomerResponse
        {
            Id = x.Id,
            BankId = x.BankId,
            Name = x.Name,
            Email = x.Email,
            Phone = x.Phone,
            CreatedAt = x.CreatedAt,
        };
}

public static class CustomerExtensions
{
    public static CustomerResponse ToResponse(this Customer x) =>
        new()
        {
            Id = x.Id,
            BankId = x.BankId,
            Name = x.Name,
            Email = x.Email,
            Phone = x.Phone,
            CreatedAt = x.CreatedAt,
        };

    public static void ApplyFields(this Customer entity, CreateCustomerRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        entity.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
    }
}

public class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }
}

public sealed class CreateAdminCustomerRequest : CreateCustomerRequest
{
    [Required]
    public Guid BankId { get; set; }
}

public sealed class UpdateCustomerRequest : CreateCustomerRequest { }
