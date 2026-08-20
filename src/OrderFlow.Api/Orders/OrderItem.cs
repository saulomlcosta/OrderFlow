using OrderFlow.Api.Common;

namespace OrderFlow.Api.Orders;

internal sealed class OrderItem
{
    internal Guid ProductId { get; private set; }

    internal string ProductName { get; private set; } = string.Empty;

    internal decimal UnitPrice { get; private set; }

    internal int Quantity { get; private set; }

    internal decimal LineTotal => UnitPrice * Quantity;

    private OrderItem()
    {
    }

    internal static OrderItem Create(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainValidationException("Order item product id must be provided.");
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainValidationException("Order item product name must be provided.");
        }

        if (unitPrice <= 0)
        {
            throw new DomainValidationException("Order item unit price must be greater than zero.");
        }

        if (quantity <= 0)
        {
            throw new DomainValidationException("Order item quantity must be greater than zero.");
        }

        return new OrderItem
        {
            ProductId = productId,
            ProductName = productName.Trim(),
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}
