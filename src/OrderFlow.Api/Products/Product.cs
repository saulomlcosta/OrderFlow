using OrderFlow.Api.Common;

namespace OrderFlow.Api.Products;

internal sealed class Product
{
    internal Guid Id { get; private set; }

    internal string Name { get; private set; } = string.Empty;

    internal decimal Price { get; private set; }

    private Product()
    {
    }

    internal static Product Create(string name, decimal price)
    {
        Validate(name, price);

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Price = price
        };
    }

    internal void UpdateCatalogDetails(string name, decimal price)
    {
        Validate(name, price);

        Name = name.Trim();
        Price = price;
    }

    private static void Validate(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Product name must not be empty.");
        }

        if (price <= 0)
        {
            throw new DomainValidationException("Product price must be greater than zero.");
        }
    }
}
