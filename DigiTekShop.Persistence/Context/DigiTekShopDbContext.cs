using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Persistence.Context;

public class DigiTekShopDbContext : DbContext
{
    public DigiTekShopDbContext(DbContextOptions<DigiTekShopDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DigiTekShopDbContext).Assembly);
    }
}