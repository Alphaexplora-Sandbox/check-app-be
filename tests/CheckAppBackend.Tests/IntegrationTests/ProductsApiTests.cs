using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CheckAppBackend.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CheckAppBackend.Tests.IntegrationTests;

[Collection("Sequential")]
public class ProductsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_ReturnsSeededCategories()
    {
        var response = await _client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.NotNull(categories);
        Assert.True(categories!.Count >= 3);
        Assert.Contains(categories, c => c.Slug == "audio");
        Assert.Contains(categories, c => c.Slug == "workspace");
        Assert.Contains(categories, c => c.Slug == "wearables");
    }

    [Fact]
    public async Task GetProducts_ReturnsSeededProducts()
    {
        var response = await _client.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        Assert.True(products!.Count >= 8);
        Assert.Contains(products, p => p.Name.Contains("Headphones"));
    }

    [Fact]
    public async Task GetProducts_FilteredByCategory_ReturnsOnlyMatchingCategory()
    {
        var response = await _client.GetAsync("/api/v1/products?category=audio");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        Assert.All(products!, p => Assert.Equal("audio", p.CategorySlug));
    }

    [Fact]
    public async Task UpdateStock_AsAdmin_UpdatesQuantitySuccessfully()
    {
        // Login as admin
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto("admin@e-commerce-mock.com", "AdminPass123!"));
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Get first product
        var productsRes = await _client.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<ProductDto>>();
        var targetProduct = products![0];

        // Patch stock
        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/products/{targetProduct.Id}/stock")
        {
            Content = JsonContent.Create(new UpdateStockDto(99))
        };
        patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var patchRes = await _client.SendAsync(patchReq);
        Assert.Equal(HttpStatusCode.OK, patchRes.StatusCode);

        var updated = await patchRes.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(updated);
        Assert.Equal(99, updated!.StockQuantity);
    }
}
