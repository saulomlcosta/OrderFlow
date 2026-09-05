using OrderFlow.Api.Checkouts;

namespace OrderFlow.Api.Common;

internal static class CheckoutMapper
{
    internal static CheckoutResponse ToResponse(this Checkout checkout, DateTimeOffset now) =>
        new(
            checkout.Id,
            checkout.CreatedAt,
            checkout.ExpiresAt,
            checkout.Status.ToString(),
            checkout.IsExpiredAt(now),
            checkout.Items
                .Select(item => new CheckoutItemResponse(item.ProductId, item.Quantity))
                .ToList());
}
