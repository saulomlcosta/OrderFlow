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

        var order = Order.Create(items, new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(450m, order.Total);
    }

    [Fact]
    public void Create_PreservesUnitPriceUsedAtCreation()
    {
        const decimal capturedPrice = 500m;
        var productId = Guid.NewGuid();
        var item = OrderItem.Create(productId, "Monitor", capturedPrice, 1);

        var order = Order.Create([item]);

        var createdItem = Assert.Single(order.Items);
        Assert.Equal(capturedPrice, createdItem.UnitPrice);
        Assert.Equal("Monitor", createdItem.ProductName);
    }
}
