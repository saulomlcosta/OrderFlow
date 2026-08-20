using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Inventory;
using OrderFlow.Api.Inventory.Data;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Orders.Data;
using OrderFlow.Api.Products;
using OrderFlow.Api.Products.Data;

namespace OrderFlow.Api.Persistence;

internal sealed class OrderFlowDbContext(DbContextOptions<OrderFlowDbContext> options)
    : DbContext(options)
{
    internal DbSet<Product> Products => Set<Product>();

    internal DbSet<Inventory.Inventory> Inventories => Set<Inventory.Inventory>();

    internal DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrderEntityConfiguration());
    }
}
