using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Utilities.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Identity.Data;

/// <summary>
/// Seeder for initial Identity setup (SuperAdmin user)
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Phone number of the SuperAdmin user (in E.164 format)
    /// </summary>
    private const string SuperAdminPhone = "+989355403605";

    /// <summary>
    /// Seeds the SuperAdmin user with SuperAdmin role.
    /// This user will have all permissions via the SuperAdmin role.
    /// </summary>
    /// <param name="userManager">UserManager instance</param>
    /// <param name="roleManager">RoleManager instance</param>
    /// <param name="logger">Optional logger</param>
    public static async Task SeedSuperAdminAsync(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger? logger = null)
    {
        logger?.LogInformation("Starting SuperAdmin seeding...");

        // Normalize phone number
        var normalizedPhone = Normalization.NormalizePhoneIranE164(SuperAdminPhone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            logger?.LogError("Invalid SuperAdmin phone number: {Phone}", SuperAdminPhone);
            return;
        }

        // 1) Ensure SuperAdmin role exists
        var superAdminRole = await roleManager.FindByNameAsync("SuperAdmin");
        if (superAdminRole is null)
        {
            superAdminRole = Role.Create("SuperAdmin");
            var roleResult = await roleManager.CreateAsync(superAdminRole);

            if (!roleResult.Succeeded)
            {
                logger?.LogError("Failed to create SuperAdmin role: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                return;
            }

            logger?.LogInformation("SuperAdmin role created");
        }

        // 2) Find or create user by normalized phone number
        var user = await userManager.Users
            .IgnoreQueryFilters() // Include deleted users for check
            .FirstOrDefaultAsync(u => u.NormalizedPhoneNumber == normalizedPhone);

        if (user is null)
        {
            // Create new user
            user = User.CreateFromPhone(SuperAdminPhone, customerId: null, phoneConfirmed: true);
            user.UserName = normalizedPhone;

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                logger?.LogError("Failed to create SuperAdmin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            logger?.LogInformation("SuperAdmin user created with phone {Phone}", normalizedPhone);
        }
        else
        {
            // User exists - ensure not deleted
            if (user.IsDeleted)
            {
                logger?.LogWarning("SuperAdmin user exists but is deleted. Phone: {Phone}", normalizedPhone);
                return;
            }

            logger?.LogInformation("SuperAdmin user already exists with phone {Phone}", normalizedPhone);
        }

        // 3) Assign SuperAdmin role if not already assigned
        if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, "SuperAdmin");
            if (!addRoleResult.Succeeded)
            {
                logger?.LogError("Failed to assign SuperAdmin role to user {UserId}: {Errors}",
                    user.Id, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                return;
            }

            logger?.LogInformation("SuperAdmin role assigned to user {UserId}", user.Id);
        }
        else
        {
            logger?.LogInformation("User {UserId} already has SuperAdmin role", user.Id);
        }

        logger?.LogInformation("✅ SuperAdmin seeding completed successfully");
    }
}

