using OrderFlow.Api.Checkouts;
using OrderFlow.Api.Common;

namespace OrderFlow.UnitTests.Checkouts;

public class CheckoutTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 8, 26, 3, 0, 0, TimeSpan.Zero);

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
        ], createdAt: StartedAt);

        checkout.Complete(StartedAt.AddMinutes(1));

        Assert.Equal(CheckoutStatus.Completed, checkout.Status);
    }

    [Fact]
    public void Cancel_WhenCheckoutIsActive_ChangesStatus()
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 1)
        ], createdAt: StartedAt);

        checkout.Cancel(StartedAt.AddMinutes(1));

        Assert.Equal(CheckoutStatus.Cancelled, checkout.Status);
    }

    [Theory]
    [InlineData((int)CheckoutStatus.Completed)]
    [InlineData((int)CheckoutStatus.Cancelled)]
    [InlineData((int)CheckoutStatus.Expired)]
    public void Complete_WhenCheckoutIsNotActive_Throws(int initialStatusValue)
    {
        var initialStatus = (CheckoutStatus)initialStatusValue;
        var checkout = CreateCheckoutInStatus(initialStatus);

        var action = () => checkout.Complete(StartedAt.AddMinutes(2));

        var exception = Assert.Throws<DomainValidationException>(action);
        Assert.Equal("Only active checkouts can be completed.", exception.Message);
        Assert.Equal(initialStatus, checkout.Status);
    }

    [Fact]
    public void Complete_WhenCheckoutIsExpired_Throws()
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 1)
        ], createdAt: StartedAt);

        var action = () => checkout.Complete(StartedAt.AddMinutes(16));

        var exception = Assert.Throws<DomainValidationException>(action);
        Assert.Equal("Expired checkouts cannot be completed.", exception.Message);
    }

    [Fact]
    public void Cancel_WhenCheckoutIsExpired_Throws()
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 1)
        ], createdAt: StartedAt);

        var action = () => checkout.Cancel(StartedAt.AddMinutes(16));

        var exception = Assert.Throws<DomainValidationException>(action);
        Assert.Equal("Expired checkouts must be released through expiration.", exception.Message);
    }

    [Theory]
    [InlineData((int)CheckoutStatus.Completed)]
    [InlineData((int)CheckoutStatus.Cancelled)]
    [InlineData((int)CheckoutStatus.Expired)]
    public void Cancel_WhenCheckoutIsNotActive_Throws(int initialStatusValue)
    {
        var initialStatus = (CheckoutStatus)initialStatusValue;
        var checkout = CreateCheckoutInStatus(initialStatus);

        var action = () => checkout.Cancel(StartedAt.AddMinutes(2));

        var exception = Assert.Throws<DomainValidationException>(action);
        Assert.Equal("Only active checkouts can be cancelled.", exception.Message);
        Assert.Equal(initialStatus, checkout.Status);
    }

    [Fact]
    public void Expire_WhenCheckoutIsOverdue_ChangesStatus()
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 1)
        ], createdAt: StartedAt);

        checkout.Expire(StartedAt.AddMinutes(16));

        Assert.Equal(CheckoutStatus.Expired, checkout.Status);
    }

    [Fact]
    public void Expire_WhenCheckoutIsNotOverdue_Throws()
    {
        var checkout = CreateCheckoutInStatus(CheckoutStatus.Active);

        var action = () => checkout.Expire(StartedAt.AddMinutes(15));

        var exception = Assert.Throws<DomainValidationException>(action);
        Assert.Equal("Only overdue checkouts can be expired.", exception.Message);
        Assert.Equal(CheckoutStatus.Active, checkout.Status);
    }

    [Theory]
    [InlineData((int)CheckoutStatus.Completed)]
    [InlineData((int)CheckoutStatus.Cancelled)]
    [InlineData((int)CheckoutStatus.Expired)]
    public void Expire_WhenCheckoutIsNotActive_Throws(int initialStatusValue)
    {
        var initialStatus = (CheckoutStatus)initialStatusValue;
        var checkout = CreateCheckoutInStatus(initialStatus);

        var action = () => checkout.Expire(StartedAt.AddMinutes(16));

        var exception = Assert.Throws<DomainValidationException>(action);
        Assert.Equal("Only active checkouts can be expired.", exception.Message);
        Assert.Equal(initialStatus, checkout.Status);
    }

    private static Checkout CreateCheckoutInStatus(CheckoutStatus status)
    {
        var checkout = Checkout.Start([
            CheckoutItem.Create(Guid.NewGuid(), 1)
        ], createdAt: StartedAt);

        switch (status)
        {
            case CheckoutStatus.Completed:
                checkout.Complete(StartedAt.AddMinutes(1));
                break;
            case CheckoutStatus.Cancelled:
                checkout.Cancel(StartedAt.AddMinutes(1));
                break;
            case CheckoutStatus.Expired:
                checkout.Expire(StartedAt.AddMinutes(16));
                break;
        }

        return checkout;
    }
}
