using TodoApi.Dtos;

namespace TodoApi.Services;

public interface IUserService
{
    Task<UserResponse> CreateUser(CreateUserRequest request);
    Task<UserResponse?> GetUser(Guid id);
    Task<IReadOnlyList<UserResponse>> SearchUsers(string? q, int take = 50);
}
