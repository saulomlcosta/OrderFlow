using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderFlow.Api.Checkouts.Data;

internal sealed class CheckoutEntityConfiguration : IEntityTypeConfiguration<Checkout>
{
    public void Configure(EntityTypeBuilder<Checkout> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.OwnsMany<CheckoutItem>("_items", item =>
        {
            item.ToTable("CheckoutItems");
            item.WithOwner().HasForeignKey("CheckoutId");
            item.Property<Guid>("Id");
            item.HasKey("Id");
            item.Property(x => x.ProductId);
            item.Property(x => x.Quantity);
        });
    }
}
