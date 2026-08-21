using OrderFlow.Api.Common;

namespace OrderFlow.Api.Checkouts;

internal sealed class CheckoutItem
{
    internal Guid ProductId { get; private set; }

    internal int Quantity { get; private set; }

    private CheckoutItem()
    {
    }

    internal static CheckoutItem Create(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainValidationException("Checkout item product id must be provided.");
        }

        if (quantity <= 0)
        {
            throw new DomainValidationException("Checkout item quantity must be greater than zero.");
        }

        return new CheckoutItem
        {
            ProductId = productId,
            Quantity = quantity
        };
    }
}
