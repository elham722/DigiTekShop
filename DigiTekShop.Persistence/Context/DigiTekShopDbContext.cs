using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Persistence.Context;

public class DigiTekShopDbContext : DbContext
{
    public DigiTekShopDbContext(DbContextOptions<DigiTekShopDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DigiTekShopDbContext).Assembly);
    }
}