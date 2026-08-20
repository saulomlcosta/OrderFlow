namespace OrderFlow.Api.Orders.CreateOrder;

internal sealed record CreateOrderRequest(IReadOnlyCollection<CreateOrderItemRequest> Items);

internal sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
