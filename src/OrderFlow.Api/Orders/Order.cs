using OrderFlow.Api.Common;

namespace OrderFlow.Api.Orders;

internal sealed class Order
{
    private readonly List<OrderItem> _items = [];

    internal Guid Id { get; private set; }

    internal DateTimeOffset CreatedAt { get; private set; }

    internal IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    internal decimal Total { get; private set; }

    private Order()
    {
    }

    internal static Order Create(IEnumerable<OrderItem> items, DateTimeOffset? createdAt = null)
    {
        var materializedItems = items.ToList();

        if (materializedItems.Count == 0)
        {
            throw new DomainValidationException("Order must contain at least one item.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow
        };

        order._items.AddRange(materializedItems);
        order.Total = materializedItems.Sum(x => x.LineTotal);

        return order;
    }
}
