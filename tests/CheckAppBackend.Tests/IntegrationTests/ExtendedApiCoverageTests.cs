using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CheckAppBackend.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CheckAppBackend.Tests.IntegrationTests;

[Collection("Sequential")]
public class ExtendedApiCoverageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExtendedApiCoverageTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CartOperations_Update_Delete_And_Clear()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto("customer@e-commerce-mock.com", "CustomerPass123!"));
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        var token = auth!.Token;

        var productsRes = await _client.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        var product = products!.First(p => p.StockQuantity > 5);

        // Add
        var addReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new AddToCartDto(product.Id, 2))
        };
        addReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var addRes = await _client.SendAsync(addReq);
        var cart = await addRes.Content.ReadFromJsonAsync<CartResponseDto>();
        Assert.NotNull(cart);
        var cartItem = cart!.Items.First(i => i.ProductId == product.Id);

        // Update quantity
        var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/cart/items/{cartItem.Id}")
        {
            Content = JsonContent.Create(new UpdateCartItemDto(5))
        };
        updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var updateRes = await _client.SendAsync(updateReq);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        // Delete single item
        var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/cart/items/{cartItem.Id}");
        delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var delRes = await _client.SendAsync(delReq);
        Assert.Equal(HttpStatusCode.OK, delRes.StatusCode);

        // Add again then clear
        var addReq2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new AddToCartDto(product.Id, 1))
        };
        addReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(addReq2);

        var clearReq = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cart");
        clearReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var clearRes = await _client.SendAsync(clearReq);
        Assert.Equal(HttpStatusCode.NoContent, clearRes.StatusCode);
    }

    [Fact]
    public async Task OrderStatus_And_AdminProduct_Operations()
    {
        var adminLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto("admin@e-commerce-mock.com", "AdminPass123!"));
        var adminAuth = await adminLoginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(adminAuth);
        var adminToken = adminAuth!.Token;

        // Categories
        var catRes = await _client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, catRes.StatusCode);
        var categories = await catRes.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.NotNull(categories);

        // Create product as admin
        var createProdReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/products")
        {
            Content = JsonContent.Create(new CreateProductDto(
                categories![0].Id,
                "Admin Test Product",
                "admin-test-product",
                "High quality engineering component",
                79.99m,
                50,
                "https://images.unsplash.com/test.jpg"
            ))
        };
        createProdReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var createProdRes = await _client.SendAsync(createProdReq);
        Assert.Equal(HttpStatusCode.Created, createProdRes.StatusCode);
        var createdProd = await createProdRes.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(createdProd);

        // Update stock
        var stockReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/products/{createdProd!.Id}/stock")
        {
            Content = JsonContent.Create(new UpdateStockDto(25))
        };
        stockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var stockRes = await _client.SendAsync(stockReq);
        Assert.Equal(HttpStatusCode.OK, stockRes.StatusCode);

        // Place an order as customer
        var custLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto("customer@e-commerce-mock.com", "CustomerPass123!"));
        var custAuth = await custLoginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
        var custToken = custAuth!.Token;

        var addReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new AddToCartDto(createdProd.Id, 1))
        };
        addReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        await _client.SendAsync(addReq);

        var shipping = new ShippingAddressDto("Jane Customer", "742 Evergreen Terrace", "Springfield", "97477");
        var checkoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new CheckoutRequestDto(shipping, "credit_card"))
        };
        checkoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var checkoutRes = await _client.SendAsync(checkoutReq);
        var order = await checkoutRes.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(order);

        // Get single order
        var getOrderReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orders/{order!.Id}");
        getOrderReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", custToken);
        var getOrderRes = await _client.SendAsync(getOrderReq);
        Assert.Equal(HttpStatusCode.OK, getOrderRes.StatusCode);

        // Update status as admin
        var updateStatusReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/orders/{order.Id}/status")
        {
            Content = JsonContent.Create(new UpdateOrderStatusDto("Shipped"))
        };
        updateStatusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var updateStatusRes = await _client.SendAsync(updateStatusReq);
        Assert.Equal(HttpStatusCode.OK, updateStatusRes.StatusCode);
    }
}
