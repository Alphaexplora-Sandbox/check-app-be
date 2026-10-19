using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CheckAppBackend.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CheckAppBackend.Tests.IntegrationTests;

[Collection("Sequential")]
public class CartAndCheckoutTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CartAndCheckoutTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteShoppingJourney_AddsToCart_CheckoutsAtomically()
    {
        // 1. Authenticate customer
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto("customer@e-commerce-mock.com", "CustomerPass123!"));
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        var token = auth!.Token;

        // 2. Fetch products to find available product
        var productsRes = await _client.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        var product = products!.First(p => p.StockQuantity > 5);
        var initialStock = product.StockQuantity;

        // 3. Add to cart
        var addReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new AddToCartDto(product.Id, 2))
        };
        addReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var addRes = await _client.SendAsync(addReq);
        Assert.Equal(HttpStatusCode.OK, addRes.StatusCode);

        var cart = await addRes.Content.ReadFromJsonAsync<CartResponseDto>();
        Assert.NotNull(cart);
        Assert.Contains(cart!.Items, i => i.ProductId == product.Id && i.Quantity >= 2);
        Assert.True(cart.Subtotal > 0);
        Assert.True(cart.EstimatedTax > 0);

        // 4. Perform atomic checkout
        var shipping = new ShippingAddressDto("Jane Customer", "742 Evergreen Terrace", "Springfield", "97477");
        var checkoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new CheckoutRequestDto(shipping, "credit_card"))
        };
        checkoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var checkoutRes = await _client.SendAsync(checkoutReq);
        Assert.Equal(HttpStatusCode.Created, checkoutRes.StatusCode);

        var order = await checkoutRes.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(order);
        Assert.StartsWith("ECM-", order!.OrderNumber);
        Assert.Equal("Processing", order.Status);
        Assert.Contains(order.OrderItems, oi => oi.ProductId == product.Id);

        // 5. Verify product stock is decremented
        var productAfterRes = await _client.GetAsync($"/api/v1/products/{product.Id}");
        var productAfter = await productAfterRes.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(productAfter);
        Assert.True(productAfter!.StockQuantity < initialStock);

        // 6. Verify cart is now empty
        var cartCheckReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cart");
        cartCheckReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var cartCheckRes = await _client.SendAsync(cartCheckReq);
        var cartAfter = await cartCheckRes.Content.ReadFromJsonAsync<CartResponseDto>();
        Assert.NotNull(cartAfter);
        Assert.Empty(cartAfter!.Items);

        // 7. Verify order exists in order history
        var ordersReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orders");
        ordersReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var ordersRes = await _client.SendAsync(ordersReq);
        var orders = await ordersRes.Content.ReadFromJsonAsync<List<OrderResponseDto>>();
        Assert.NotNull(orders);
        Assert.Contains(orders!, o => o.OrderNumber == order.OrderNumber);
    }
}
