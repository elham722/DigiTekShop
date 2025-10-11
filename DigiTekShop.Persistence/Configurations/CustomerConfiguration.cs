using DigiTekShop.Domain.Customer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        
        b.ToTable("Customers");

        
        b.HasKey(x => x.Id);
        b.Property(x => x.Id)
            .HasConversion(
                id => id.Value,          
                v => new CustomerId(v)) 
            .ValueGeneratedNever();

        
        b.Property(x => x.UserId)
            .IsRequired();

        b.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        b.Property(x => x.Phone)
            .HasMaxLength(30);

        b.Property(x => x.IsActive)
            .HasDefaultValue(true);

        
        b.Property(x => x.CreatedAtUtc)
            .IsRequired();

        b.Property(x => x.UpdatedAtUtc);

      
        b.Property(x => x.Version)
            .IsRowVersion()
            .IsConcurrencyToken();

        
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Email).IsUnique();

         b.Ignore(x => x.DomainEvents);

       
        b.Metadata.FindNavigation(nameof(Customer.Addresses))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(x => x.Addresses, a =>
        {
            a.ToTable("CustomerAddresses");

            a.WithOwner().HasForeignKey("CustomerId");

          
            a.Property<int>("Id");
            a.HasKey("Id");

            a.Property(p => p.Line1).IsRequired().HasMaxLength(200);
            a.Property(p => p.Line2).HasMaxLength(200);
            a.Property(p => p.City).IsRequired().HasMaxLength(100);
            a.Property(p => p.State).HasMaxLength(100);
            a.Property(p => p.PostalCode).IsRequired().HasMaxLength(20);
            a.Property(p => p.Country).IsRequired().HasMaxLength(100);
            a.Property(p => p.IsDefault).IsRequired();

            
            a.HasIndex("CustomerId");
            a.HasIndex(p => new { p.PostalCode, p.City });

            
            a.HasIndex("CustomerId", nameof(DigiTekShop.Domain.Customer.ValueObjects.Address.IsDefault))
             .IsUnique()
             .HasFilter("[IsDefault] = 1");
        });
    }
}
