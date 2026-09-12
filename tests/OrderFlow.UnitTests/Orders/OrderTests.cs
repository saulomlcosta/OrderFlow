using OrderFlow.Api.Common;
using OrderFlow.Api.Orders;

namespace OrderFlow.UnitTests.Orders;

public class OrderTests
{
    private const string CustomerSubject = "customer-subject";

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
            CustomerSubject,
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

        var order = Order.Create(Guid.NewGuid(), CustomerSubject, [item]);

        var createdItem = Assert.Single(order.Items);
        Assert.Equal(capturedPrice, createdItem.UnitPrice);
        Assert.Equal("Monitor", createdItem.ProductName);
    }

    [Fact]
    public void Create_PreservesOriginatingCheckout()
    {
        var checkoutId = Guid.NewGuid();
        var item = OrderItem.Create(Guid.NewGuid(), "Desk", 800m, 1);

        var order = Order.Create(checkoutId, CustomerSubject, [item]);

        Assert.Equal(checkoutId, order.CheckoutId);
        Assert.Equal(CustomerSubject, order.CustomerSubject);
    }

    [Fact]
    public void Create_WithoutCustomerSubject_Throws()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Desk", 800m, 1);

        var exception = Assert.Throws<DomainValidationException>(() =>
            Order.Create(Guid.NewGuid(), " ", [item]));

        Assert.Equal("Order must reference a customer subject.", exception.Message);
    }

    [Fact]
    public void Create_WithoutValidCheckout_Throws()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "Desk", 800m, 1);

        var exception = Assert.Throws<DomainValidationException>(() =>
            Order.Create(Guid.Empty, CustomerSubject, [item]));

        Assert.Equal("Order must reference a valid checkout.", exception.Message);
    }
}
