namespace DigiTekShop.Identity.UnitTests.Helpers;

/// <summary>
/// Helper for creating in-memory database context for testing
/// </summary>
public static class InMemoryDbHelper
{
    public static DigiTekShopIdentityDbContext CreateInMemoryContext(string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        
        var options = new DbContextOptionsBuilder<DigiTekShopIdentityDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new DigiTekShopIdentityDbContext(options);
    }

    public static async Task<User> SeedUserAsync(
        DigiTekShopIdentityDbContext context, 
        string phoneOrEmail = "+989121234567",
        bool isVerified = true)
    {
        // ساخت user با phone (چون فقط CreateFromPhone داریم)
        var user = User.CreateFromPhone(phoneOrEmail, customerId: null, phoneConfirmed: isVerified);
        
        // اگر email هست، اضافه‌ش کنیم
        if (phoneOrEmail.Contains("@"))
        {
            user.Email = phoneOrEmail;
            user.EmailConfirmed = isVerified;
            user.UserName = phoneOrEmail;
            user.NormalizedUserName = phoneOrEmail.ToUpperInvariant();
            user.NormalizedEmail = phoneOrEmail.ToUpperInvariant();
        }
        
        // Set password hash
        user.PasswordHash = "AQAAAAIAAYagAAAAEDummyHashForTestingPurposesOnly==";

        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        return user;
    }
}

