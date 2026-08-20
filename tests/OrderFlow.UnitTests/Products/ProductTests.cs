using OrderFlow.Api.Common;
using OrderFlow.Api.Products;

namespace OrderFlow.UnitTests.Products;

public class ProductTests
{
    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var action = () => Product.Create(string.Empty, 10m);

        var exception = Assert.Throws<DomainValidationException>(action);

        Assert.Equal("Product name must not be empty.", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidPrice_Throws()
    {
        var action = () => Product.Create("Keyboard", 0m);

        var exception = Assert.Throws<DomainValidationException>(action);

        Assert.Equal("Product price must be greater than zero.", exception.Message);
    }
}
