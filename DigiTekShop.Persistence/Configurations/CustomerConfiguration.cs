using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DigiTekShop.Domain.Customers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigiTekShop.Persistence.Configurations
{
    public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> e)
        {
           
            e.HasKey(x => x.Id);
            e.Property(x => x.Id)
                .ValueGeneratedNever()
                .HasConversion(v => v.Value, v => new CustomerId(v));

            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(128).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsUnicode(false).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(32).IsUnicode(false);

            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();

            e.OwnsMany(x => x.Addresses, nav =>
            {
                nav.WithOwner().HasForeignKey("CustomerId");
                nav.Property<int>("Id");
                nav.HasKey("Id");

                nav.Property(a => a.Line1).HasMaxLength(256).IsRequired();
                nav.Property(a => a.Line2).HasMaxLength(256);
                nav.Property(a => a.City).HasMaxLength(128).IsRequired();
                nav.Property(a => a.State).HasMaxLength(128);
                nav.Property(a => a.PostalCode).HasMaxLength(32).IsRequired();
                nav.Property(a => a.Country).HasMaxLength(64).IsRequired();
                nav.Property(a => a.IsDefault).IsRequired();
            });
        }
    }
}
