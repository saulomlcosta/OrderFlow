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

    internal string? CustomerSubject { get; private set; }

    internal IReadOnlyCollection<CheckoutItem> Items => _items.AsReadOnly();

    private Checkout()
    {
    }

    internal static Checkout Start(
        string customerSubject,
        IEnumerable<CheckoutItem> items,
        DateTimeOffset? createdAt = null,
        TimeSpan? reservationDuration = null)
    {
        if (string.IsNullOrWhiteSpace(customerSubject))
        {
            throw new DomainValidationException("Checkout must reference a customer subject.");
        }

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
            Status = CheckoutStatus.Active,
            CustomerSubject = customerSubject
        };

        checkout._items.AddRange(materializedItems);
        return checkout;
    }

    internal bool IsExpiredAt(DateTimeOffset now) =>
        Status == CheckoutStatus.Expired ||
        (Status == CheckoutStatus.Active && ExpiresAt < now);

    internal void Complete(DateTimeOffset now)
    {
        if (Status != CheckoutStatus.Active)
        {
            throw new DomainValidationException("Only active checkouts can be completed.");
        }

        if (IsExpiredAt(now))
        {
            throw new DomainValidationException("Expired checkouts cannot be completed.");
        }

        Status = CheckoutStatus.Completed;
    }

    internal void Cancel(DateTimeOffset now)
    {
        if (Status != CheckoutStatus.Active)
        {
            throw new DomainValidationException("Only active checkouts can be cancelled.");
        }

        if (IsExpiredAt(now))
        {
            throw new DomainValidationException("Expired checkouts must be released through expiration.");
        }

        Status = CheckoutStatus.Cancelled;
    }

    internal void Expire(DateTimeOffset now)
    {
        if (Status != CheckoutStatus.Active)
        {
            throw new DomainValidationException("Only active checkouts can be expired.");
        }

        if (ExpiresAt >= now)
        {
            throw new DomainValidationException("Only overdue checkouts can be expired.");
        }

        Status = CheckoutStatus.Expired;
    }
}
