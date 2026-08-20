namespace OrderFlow.Api.Products;

internal sealed record ProductResponse(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity);
