using OrderFlow.Api.Common;

namespace OrderFlow.Api.Inventory;

internal sealed class Inventory
{
    internal Guid ProductId { get; private set; }

    internal int Quantity { get; private set; }

    private Inventory()
    {
    }

    internal static Inventory ForProduct(Guid productId)
    {
        return new Inventory
        {
            ProductId = productId,
            Quantity = 0
        };
    }

    internal void Add(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Stock quantity to add must be greater than zero.");
        }

        Quantity += quantity;
    }

    internal void Remove(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Stock quantity to remove must be greater than zero.");
        }

        if (quantity > Quantity)
        {
            throw new DomainValidationException("Cannot remove more stock than is available.");
        }

        Quantity -= quantity;
    }
}
