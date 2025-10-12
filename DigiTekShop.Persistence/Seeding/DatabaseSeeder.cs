using DigiTekShop.Domain.Customer.Entities;
using DigiTekShop.Domain.Customer.ValueObjects;
using DigiTekShop.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Persistence.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigiTekShopDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await context.Customers.AnyAsync())
            {
                logger.LogInformation("Database already seeded");
                return;
            }

            // Seed sample customers
            var customers = new List<Customer>
            {
                Customer.Register(
                    userId: Guid.NewGuid(),
                    fullName: "John Doe",
                    email: "john.doe@example.com",
                    phone: "+1234567890"
                ),
                Customer.Register(
                    userId: Guid.NewGuid(),
                    fullName: "Jane Smith",
                    email: "jane.smith@example.com",
                    phone: "+0987654321"
                ),
                Customer.Register(
                    userId: Guid.NewGuid(),
                    fullName: "Bob Johnson",
                    email: "bob.johnson@example.com",
                    phone: "+1122334455"
                )
            };

            // Add addresses to customers
            var addresses = new List<Address>
            {
                new Address(
                    line1: "123 Main St",
                    line2: "Apt 4B",
                    city: "New York",
                    state: "NY",
                    postalCode: "10001",
                    country: "USA",
                    isDefault: true
                ),
                new Address(
                    line1: "456 Oak Ave",
                    line2: null,
                    city: "Los Angeles",
                    state: "CA",
                    postalCode: "90210",
                    country: "USA",
                    isDefault: false
                ),
                new Address(
                    line1: "789 Pine St",
                    line2: "Suite 200",
                    city: "Chicago",
                    state: "IL",
                    postalCode: "60601",
                    country: "USA",
                    isDefault: true
                )
            };

            // Add addresses to customers
            for (int i = 0; i < customers.Count; i++)
            {
                customers[i].AddAddress(addresses[i], i == 0); // First customer gets default address
            }

            // Add customers to context
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();

            logger.LogInformation("Database seeded successfully with {CustomerCount} customers", customers.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
