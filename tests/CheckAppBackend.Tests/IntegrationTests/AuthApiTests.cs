using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CheckAppBackend.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CheckAppBackend.Tests.IntegrationTests;

public class AuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtAndUser()
    {
        var loginReq = new LoginRequestDto("customer@e-commerce-mock.com", "CustomerPass123!");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.Token));
        Assert.Equal("customer@e-commerce-mock.com", result.User.Email);
        Assert.Equal("Customer Demo", result.User.FullName);
        Assert.Equal("Customer", result.User.Role);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginReq = new LoginRequestDto("customer@e-commerce-mock.com", "WrongPassword!");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithNewAccount_ReturnsCreatedAndJwt()
    {
        var uniqueEmail = $"user_{Guid.NewGuid().ToString()[..8]}@example.com";
        var regReq = new RegisterRequestDto(uniqueEmail, "ValidPass123!", "Jane Doe");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", regReq);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.Token));
        Assert.Equal(uniqueEmail, result.User.Email);
        Assert.Equal("Jane Doe", result.User.FullName);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsCurrentUserProfile()
    {
        var loginReq = new LoginRequestDto("customer@e-commerce-mock.com", "CustomerPass123!");
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var meRes = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        var user = await meRes.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal("customer@e-commerce-mock.com", user!.Email);
    }
}
