using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CheckAppBackend.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CheckAppBackend.Tests.IntegrationTests;

[Collection("Sequential")]
public class PaymentAndOrderTrackingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PaymentAndOrderTrackingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetCustomerTokenAsync()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto("customer@e-commerce-mock.com", "CustomerPass123!"));
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        return auth!.Token;
    }

    [Fact]
    public async Task GetPaymentMethods_ReturnsSavedCards()
    {
        var token = await GetCustomerTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/payments/methods");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var methods = await res.Content.ReadFromJsonAsync<List<PaymentMethodResponseDto>>();
        Assert.NotNull(methods);
        Assert.NotEmpty(methods!);
        Assert.Contains(methods!, m => m.CardNumberMasked.Contains("4242"));
    }

    [Fact]
    public async Task CreateAndManagePaymentMethod_SavesMaskedCard_AndSetsDefault()
    {
        var token = await GetCustomerTokenAsync();

        // 1. Create a new card
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/methods")
        {
            Content = JsonContent.Create(new CreatePaymentMethodDto(
                "Customer Demo",
                "5555444433332222",
                "09",
                "2029",
                "Mastercard",
                false
            ))
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var created = await createRes.Content.ReadFromJsonAsync<PaymentMethodResponseDto>();
        Assert.NotNull(created);
        Assert.Contains("2222", created!.CardNumberMasked);
        Assert.Equal("Mastercard", created.Brand);

        // 2. Set as default
        var defReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/payments/methods/{created.Id}/default");
        defReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var defRes = await _client.SendAsync(defReq);
        Assert.Equal(HttpStatusCode.OK, defRes.StatusCode);

        // 3. Delete card
        var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/payments/methods/{created.Id}");
        delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var delRes = await _client.SendAsync(delReq);
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_RestoresInventoryStock_AndSetsStatusCancelled()
    {
        var token = await GetCustomerTokenAsync();

        // 1. Add item to cart
        var productsRes = await _client.GetAsync("/api/v1/products");
        var products = await productsRes.Content.ReadFromJsonAsync<List<ProductDto>>();
        var product = products!.First(p => p.StockQuantity > 5);
        var originalStock = product.StockQuantity;

        var addReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new AddToCartDto(product.Id, 2))
        };
        addReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(addReq);

        // 2. Checkout
        var checkoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new CheckoutRequestDto(
                new ShippingAddressDto("Jane Customer", "742 Evergreen", "Springfield", "97477"),
                "credit_card"
            ))
        };
        checkoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var checkoutRes = await _client.SendAsync(checkoutReq);
        var order = await checkoutRes.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(order);

        // Verify stock decremented
        var productAfterCheckout = (await (await _client.GetAsync($"/api/v1/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal(originalStock - 2, productAfterCheckout.StockQuantity);

        // 3. Cancel the order
        var cancelReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orders/{order!.Id}/cancel")
        {
            Content = JsonContent.Create(new CancelOrderRequestDto("Changed my mind"))
        };
        cancelReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var cancelRes = await _client.SendAsync(cancelReq);
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        var cancelledOrder = await cancelRes.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotNull(cancelledOrder);
        Assert.Equal("Cancelled", cancelledOrder!.Status);

        // Verify stock was restored
        var productAfterCancel = (await (await _client.GetAsync($"/api/v1/products/{product.Id}")).Content.ReadFromJsonAsync<ProductDto>())!;
        Assert.Equal(originalStock, productAfterCancel.StockQuantity);

        // 4. Verify tracking milestones
        var trackReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orders/{order.Id}/track");
        trackReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var trackRes = await _client.SendAsync(trackReq);
        Assert.Equal(HttpStatusCode.OK, trackRes.StatusCode);

        var tracking = await trackRes.Content.ReadFromJsonAsync<OrderTrackingResponseDto>();
        Assert.NotNull(tracking);
        Assert.Equal("Cancelled", tracking!.Status);
        Assert.Contains(tracking.Milestones, m => m.Title == "Order Cancelled");
    }
}
