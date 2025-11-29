using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Utilities.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DigiTekShop.Identity.Data;

public static class IdentitySeeder
{
   
    private const string SuperAdminPhone = "+989355403605";

    public static async Task SeedSuperAdminAsync(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger? logger = null)
    {
        logger?.LogInformation("Starting SuperAdmin seeding...");

        var normalizedPhone = Normalization.NormalizePhoneIranE164(SuperAdminPhone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            logger?.LogError("Invalid SuperAdmin phone number: {Phone}", SuperAdminPhone);
            return;
        }

        var superAdminRole = await roleManager.FindByNameAsync("SuperAdmin");
        if (superAdminRole is null)
        {
            superAdminRole = Role.Create(
                "SuperAdmin",
                description: "دسترسی کامل به تمام بخش‌های سیستم. این نقش قابل حذف نیست.",
                isSystemRole: true,
                isDefaultForNewUsers: false);
            var roleResult = await roleManager.CreateAsync(superAdminRole);

            if (!roleResult.Succeeded)
            {
                logger?.LogError("Failed to create SuperAdmin role: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                return;
            }

            logger?.LogInformation("SuperAdmin role created");
        }

        
        var user = await userManager.Users
            .IgnoreQueryFilters() 
            .FirstOrDefaultAsync(u => u.NormalizedPhoneNumber == normalizedPhone);

        if (user is null)
        {
            
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
           
            if (user.IsDeleted)
            {
                logger?.LogWarning("SuperAdmin user exists but is deleted. Phone: {Phone}", normalizedPhone);
                return;
            }

            logger?.LogInformation("SuperAdmin user already exists with phone {Phone}", normalizedPhone);
        }

        
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

