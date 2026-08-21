using OrderFlow.Api.Common;

namespace OrderFlow.Api.Inventory;

internal sealed class Inventory
{
    internal Guid ProductId { get; private set; }

    internal int Quantity { get; private set; }

    internal int ReservedQuantity { get; private set; }

    internal int AvailableQuantity => Quantity - ReservedQuantity;

    private Inventory()
    {
    }

    internal static Inventory ForProduct(Guid productId)
    {
        return new Inventory
        {
            ProductId = productId,
            Quantity = 0,
            ReservedQuantity = 0
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

        if (quantity > AvailableQuantity)
        {
            throw new DomainValidationException("Cannot remove more stock than is available.");
        }

        Quantity -= quantity;
    }

    internal void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Stock quantity to reserve must be greater than zero.");
        }

        if (quantity > AvailableQuantity)
        {
            throw new DomainValidationException("Cannot reserve more stock than is available.");
        }

        ReservedQuantity += quantity;
    }

    internal void Release(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Stock quantity to release must be greater than zero.");
        }

        if (quantity > ReservedQuantity)
        {
            throw new DomainValidationException("Cannot release more stock than is reserved.");
        }

        ReservedQuantity -= quantity;
    }

    internal void ConfirmReservedRemoval(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Stock quantity to confirm must be greater than zero.");
        }

        if (quantity > ReservedQuantity)
        {
            throw new DomainValidationException("Cannot confirm more reserved stock than is reserved.");
        }

        if (quantity > Quantity)
        {
            throw new DomainValidationException("Cannot confirm more stock than exists.");
        }

        ReservedQuantity -= quantity;
        Quantity -= quantity;
    }
}
