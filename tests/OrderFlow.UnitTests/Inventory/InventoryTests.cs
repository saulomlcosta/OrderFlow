using OrderFlow.Api.Common;

namespace OrderFlow.UnitTests.Inventory;

public class InventoryTests
{
    [Fact]
    public void Add_WithValidQuantity_IncreasesStock()
    {
        var inventory = OrderFlow.Api.Inventory.Inventory.ForProduct(Guid.NewGuid());

        inventory.Add(5);

        Assert.Equal(5, inventory.Quantity);
    }

    [Fact]
    public void Add_WithInvalidQuantity_Throws()
    {
        var inventory = OrderFlow.Api.Inventory.Inventory.ForProduct(Guid.NewGuid());

        var action = () => inventory.Add(0);

        var exception = Assert.Throws<DomainValidationException>(action);

        Assert.Equal("Stock quantity to add must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Remove_WithValidQuantity_DecreasesStock()
    {
        var inventory = OrderFlow.Api.Inventory.Inventory.ForProduct(Guid.NewGuid());
        inventory.Add(5);

        inventory.Remove(3);

        Assert.Equal(2, inventory.Quantity);
    }

    [Fact]
    public void Remove_MoreThanAvailable_Throws()
    {
        var inventory = OrderFlow.Api.Inventory.Inventory.ForProduct(Guid.NewGuid());
        inventory.Add(2);

        var action = () => inventory.Remove(3);

        var exception = Assert.Throws<DomainValidationException>(action);

        Assert.Equal("Cannot remove more stock than is available.", exception.Message);
        Assert.Equal(2, inventory.Quantity);
    }
}
