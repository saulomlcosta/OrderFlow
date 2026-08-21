namespace OrderFlow.Api.Checkouts.StartCheckout;

internal sealed record StartCheckoutRequest(IReadOnlyCollection<StartCheckoutItemRequest> Items);

internal sealed record StartCheckoutItemRequest(Guid ProductId, int Quantity);
