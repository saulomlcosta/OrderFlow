using OrderFlow.Api.Common;
using OrderFlow.Api.Orders;

namespace OrderFlow.UnitTests.Orders;

public class OrderTests
{
    [Fact]
    public void Create_CalculatesTotalCorrectly()
    {
        var items = new[]
        {
            OrderItem.Create(Guid.NewGuid(), "Mouse", 100m, 2),
            OrderItem.Create(Guid.NewGuid(), "Keyboard", 250m, 1)
        };

        var order = Order.Create(
            Guid.NewGuid(),
            items,
            new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(450m, order.Total);
    }

    [Fact]
    public void Create_PreservesUnitPriceUsedAtCreation()
    {
        const decimal capturedPrice = 500m;
        var productId = Guid.NewGuid();
        var item = OrderItem.Create(productId, "Monitor", capturedPrice, 1);

        var order = Order.Create(Guid.NewGuid(), [item]);

        var createdItem = Assert.Single(order.Items);
        Assert.Equal(capturedPrice, createdItem.UnitPrice);
        Assert.Equal("Monitor", createdItem.ProductName);
    }

    [Fact]
    public void Create_PreservesOriginatingCheckout()
    {
        var checkoutId = Guid.NewGuid();
        var item = OrderItem.Create(Guid.NewGuid(), "Desk", 800m, 1);

        var order = Order.Create(checkoutId, [item]);

        Assert.Equal(checkoutId, order.CheckoutId);
    }

    [Fact]
    public void Create_WithoutValidCheckout_Throws()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Desk", 800m, 1);

        var exception = Assert.Throws<DomainValidationException>(() =>
            Order.Create(Guid.Empty, [item]));

        Assert.Equal("Order must reference a valid checkout.", exception.Message);
    }
}
