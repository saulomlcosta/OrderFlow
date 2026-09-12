using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Api.Checkouts;

namespace OrderFlow.Api.Orders.Data;

internal sealed class OrderEntityConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CheckoutId);
        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.CustomerSubject).HasMaxLength(200);
        builder.HasIndex(x => x.CustomerSubject);

        builder.HasOne<Checkout>()
            .WithOne()
            .HasForeignKey<Order>(x => x.CheckoutId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CheckoutId)
            .IsUnique()
            .HasFilter("\"CheckoutId\" IS NOT NULL");

        builder.OwnsMany<OrderItem>("_items", item =>
        {
            item.ToTable("OrderItems");
            item.WithOwner().HasForeignKey("OrderId");
            item.Property<Guid>("Id");
            item.HasKey("Id");
            item.Property(x => x.ProductId);
            item.Property(x => x.ProductName).HasMaxLength(200);
            item.Property(x => x.UnitPrice).HasPrecision(18, 2);
            item.Property(x => x.Quantity);
        });
    }
}
