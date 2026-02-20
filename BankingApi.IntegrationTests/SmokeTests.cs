using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BankingApi.IntegrationTests;

/// <summary>
/// Smoke tests for the live Multi-Tenant Banking API.
///
/// By default these run against the deployed Fly.io instance.
/// Set the BASE_URL environment variable to target a local instance:
///
///   BASE_URL=http://localhost:5000 dotnet test
/// </summary>
public class SmokeTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("BASE_URL") ?? "https://dotnet-db-multi-tenant-demo.fly.dev";

    private static readonly HttpClient Http = new() { BaseAddress = new Uri(BaseUrl) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // -------------------------------------------------------------------------
    // DTOs
    // -------------------------------------------------------------------------

    private record SeededLogins(
        string AdminEmail,
        string StaffBankAEmail,
        string StaffBankBEmail,
        string CustomerAEmail,
        string CustomerBEmail,
        string BankAId,
        string BankBId,
        string CustomerAId,
        string CustomerBId
    );

    private record LoginRequest(string Email);

    private record LoginResponse(string AccessToken, string TokenType, string Role, string? BankId, string? CustomerId);

    private record Bank(string Id, string Name, string Code);

    private record Customer(string Id, string BankId, string Email);

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<SeededLogins> GetSeededLoginsAsync()
    {
        var res = await Http.GetAsync("/api/auth/seeded-logins");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<SeededLogins>(JsonOptions))!;
    }

    private async Task<LoginResponse> LoginAsync(string email)
    {
        var res = await Http.PostAsJsonAsync("/api/auth/login", new LoginRequest(email));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token
        );
        return client;
    }

    // -------------------------------------------------------------------------
    // Landing page & Scalar UI
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LandingPage_Returns200_WithExpectedTitle()
    {
        var res = await Http.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("Multi-Tenant Banking API", html);
    }

    [Fact]
    public async Task ScalarUi_Returns200()
    {
        var res = await Http.GetAsync("/scalar/v1");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Anonymous endpoint
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SeededLogins_ReturnsAllEmailsAndIds_WithoutAuth()
    {
        var logins = await GetSeededLoginsAsync();

        Assert.Equal("admin@demo.com", logins.AdminEmail);
        Assert.Equal("staff.norge@demo.com", logins.StaffBankAEmail);
        Assert.Equal("staff.svensk@demo.com", logins.StaffBankBEmail);
        Assert.Equal("customer.ola@demo.com", logins.CustomerAEmail);
        Assert.Equal("customer.anna@demo.com", logins.CustomerBEmail);
        Assert.Matches(@"^[0-9a-f\-]{36}$", logins.BankAId);
        Assert.Matches(@"^[0-9a-f\-]{36}$", logins.BankBId);
        Assert.Matches(@"^[0-9a-f\-]{36}$", logins.CustomerAId);
        Assert.Matches(@"^[0-9a-f\-]{36}$", logins.CustomerBId);
    }

    // -------------------------------------------------------------------------
    // Auth
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_Admin_ReturnsJwtWithAdminRole()
    {
        var token = await LoginAsync("admin@demo.com");

        Assert.NotEmpty(token.AccessToken);
        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal("Admin", token.Role);
        Assert.Null(token.BankId);
    }

    [Fact]
    public async Task Login_BankAStaff_ReturnsStaffRoleWithCorrectBankId()
    {
        var logins = await GetSeededLoginsAsync();
        var token = await LoginAsync("staff.norge@demo.com");

        Assert.Equal("Staff", token.Role);
        Assert.Equal(logins.BankAId, token.BankId);
    }

    // -------------------------------------------------------------------------
    // Admin endpoints
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetBanks_WithAdminToken_ReturnsBothBanks()
    {
        var token = await LoginAsync("admin@demo.com");
        using var client = AuthenticatedClient(token.AccessToken);

        var res = await client.GetAsync("/api/admin/banks");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var banks = (await res.Content.ReadFromJsonAsync<Bank[]>(JsonOptions))!;
        Assert.Equal(2, banks.Length);
        Assert.Contains(banks, b => b.Name == "Norge Bank");
        Assert.Contains(banks, b => b.Name == "Svensk Bank");
    }

    [Fact]
    public async Task GetBanks_WithoutToken_Returns401()
    {
        var res = await Http.GetAsync("/api/admin/banks");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Tenant isolation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BankAStaff_SeesOnlyTheirCustomer()
    {
        var logins = await GetSeededLoginsAsync();
        var token = await LoginAsync("staff.norge@demo.com");
        using var client = AuthenticatedClient(token.AccessToken);

        var res = await client.GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var customers = (await res.Content.ReadFromJsonAsync<Customer[]>(JsonOptions))!;
        Assert.Single(customers);
        Assert.Equal("customer.ola@demo.com", customers[0].Email);
        Assert.Equal(logins.BankAId, customers[0].BankId);
    }

    [Fact]
    public async Task BankBStaff_SeesOnlyTheirCustomer()
    {
        var logins = await GetSeededLoginsAsync();
        var token = await LoginAsync("staff.svensk@demo.com");
        using var client = AuthenticatedClient(token.AccessToken);

        var res = await client.GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var customers = (await res.Content.ReadFromJsonAsync<Customer[]>(JsonOptions))!;
        Assert.Single(customers);
        Assert.Equal("customer.anna@demo.com", customers[0].Email);
        Assert.Equal(logins.BankBId, customers[0].BankId);
    }

    [Fact]
    public async Task BankAStaff_CannotAccessBankBCustomer_Returns404()
    {
        var logins = await GetSeededLoginsAsync();
        var token = await LoginAsync("staff.norge@demo.com");
        using var client = AuthenticatedClient(token.AccessToken);

        var res = await client.GetAsync($"/api/customers/{logins.CustomerBId}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task BankBStaff_CannotAccessBankACustomer_Returns404()
    {
        var logins = await GetSeededLoginsAsync();
        var token = await LoginAsync("staff.svensk@demo.com");
        using var client = AuthenticatedClient(token.AccessToken);

        var res = await client.GetAsync($"/api/customers/{logins.CustomerAId}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
