namespace OrderFlow.Api.Orders;

internal sealed record OrderResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    decimal Total,
    IReadOnlyCollection<OrderItemResponse> Items);

internal sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);
