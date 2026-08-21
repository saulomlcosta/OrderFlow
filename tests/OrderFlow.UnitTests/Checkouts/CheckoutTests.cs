using OrderFlow.Api.Checkouts;
using OrderFlow.Api.Common;

namespace OrderFlow.UnitTests.Checkouts;

public class CheckoutTests
{
    [Fact]
    public void Start_WithItems_CreatesActiveCheckout()
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 2)
        ]);

        Assert.Equal(CheckoutStatus.Active, checkout.Status);
        Assert.Single(checkout.Items);
    }

    [Fact]
    public void Complete_WhenCheckoutIsActive_ChangesStatus()
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 1)
        ]);

        checkout.Complete();

        Assert.Equal(CheckoutStatus.Completed, checkout.Status);
    }

    [Fact]
    public void Cancel_WhenCheckoutIsActive_ChangesStatus()
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 1)
        ]);

        checkout.Cancel();

        Assert.Equal(CheckoutStatus.Cancelled, checkout.Status);
    }

    [Fact]
    public void Complete_WhenCheckoutIsNotActive_Throws()
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 1)
        ]);
        checkout.Cancel();

        var action = () => checkout.Complete();

        var exception = Assert.Throws<DomainValidationException>(action);
        Assert.Equal("Only active checkouts can be completed.", exception.Message);
    }
}
