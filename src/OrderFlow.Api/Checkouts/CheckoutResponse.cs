namespace OrderFlow.Api.Checkouts;

internal sealed record CheckoutResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string Status,
    bool IsExpired,
    IReadOnlyCollection<CheckoutItemResponse> Items);

internal sealed record CheckoutItemResponse(Guid ProductId, int Quantity);
