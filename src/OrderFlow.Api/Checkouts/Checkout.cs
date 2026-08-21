using OrderFlow.Api.Common;

namespace OrderFlow.Api.Checkouts;

internal sealed class Checkout
{
    private static readonly TimeSpan DefaultReservationDuration = TimeSpan.FromMinutes(15);
    private readonly List<CheckoutItem> _items = [];

    internal Guid Id { get; private set; }

    internal DateTimeOffset CreatedAt { get; private set; }

    internal DateTimeOffset ExpiresAt { get; private set; }

    internal CheckoutStatus Status { get; private set; }

    internal IReadOnlyCollection<CheckoutItem> Items => _items.AsReadOnly();

    private Checkout()
    {
    }

    internal static Checkout Start(
        IEnumerable<CheckoutItem> items,
        DateTimeOffset? createdAt = null,
        TimeSpan? reservationDuration = null)
    {
        var materializedItems = items.ToList();

        if (materializedItems.Count == 0)
        {
            throw new DomainValidationException("Checkout must contain at least one item.");
        }

        var startedAt = createdAt ?? DateTimeOffset.UtcNow;
        var duration = reservationDuration ?? DefaultReservationDuration;

        if (duration <= TimeSpan.Zero)
        {
            throw new DomainValidationException("Checkout reservation duration must be greater than zero.");
        }

        var checkout = new Checkout
        {
            Id = Guid.NewGuid(),
            CreatedAt = startedAt,
            ExpiresAt = startedAt.Add(duration),
            Status = CheckoutStatus.Active
        };

        checkout._items.AddRange(materializedItems);
        return checkout;
    }

    internal void Complete()
    {
        if (Status != CheckoutStatus.Active)
        {
            throw new DomainValidationException("Only active checkouts can be completed.");
        }

        Status = CheckoutStatus.Completed;
    }

    internal void Cancel()
    {
        if (Status != CheckoutStatus.Active)
        {
            throw new DomainValidationException("Only active checkouts can be cancelled.");
        }

        Status = CheckoutStatus.Cancelled;
    }
}
