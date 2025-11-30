using DigiTekShop.Contracts.Abstractions.Profile;
using DigiTekShop.Contracts.DTOs.Profile;
using DigiTekShop.SharedKernel.Errors;
using DigiTekShop.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace DigiTekShop.Infrastructure.Services;

/// <summary>
/// سرویس مدیریت پروفایل کاربر
/// از Abstractions استفاده می‌کند - بدون وابستگی مستقیم به DbContext
/// </summary>
public sealed class ProfileService : IProfileService
{
    private readonly IUserProfileReader _userReader;
    private readonly ICustomerProfileRepository _customerRepo;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IUserProfileReader userReader,
        ICustomerProfileRepository customerRepo,
        ILogger<ProfileService> logger)
    {
        _userReader = userReader;
        _customerRepo = customerRepo;
        _logger = logger;
    }

    public async Task<Result<ProfileCompletionStatus>> GetCompletionStatusAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var userData = await _userReader.GetUserDataAsync(userId, ct);

        if (userData is null)
        {
            return Result<ProfileCompletionStatus>.Failure(
                "کاربر یافت نشد",
                ErrorCodes.Identity.USER_NOT_FOUND);
        }

        // اگر CustomerId ندارد، پروفایل ناقص است
        if (!userData.CustomerId.HasValue)
        {
            return Result<ProfileCompletionStatus>.Success(
                ProfileCompletionStatus.Incomplete(
                    missingFields: ["FullName"],
                    percentage: 0));
        }

        // چک کردن اینکه Customer واقعاً وجود دارد
        var customerExists = await _customerRepo.ExistsAsync(userData.CustomerId.Value, ct);

        if (!customerExists)
        {
            _logger.LogWarning(
                "User {UserId} has CustomerId {CustomerId} but Customer does not exist",
                userId, userData.CustomerId);

            return Result<ProfileCompletionStatus>.Success(
                ProfileCompletionStatus.Incomplete(
                    missingFields: ["FullName"],
                    percentage: 0));
        }

        return Result<ProfileCompletionStatus>.Success(
            ProfileCompletionStatus.Complete(userData.CustomerId.Value));
    }

    public async Task<Result<ProfileDto>> GetProfileAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var userData = await _userReader.GetUserDataAsync(userId, ct);

        if (userData is null)
        {
            return Result<ProfileDto>.Failure(
                "کاربر یافت نشد",
                ErrorCodes.Identity.USER_NOT_FOUND);
        }

        string? fullName = null;
        string? customerEmail = null;

        if (userData.CustomerId.HasValue)
        {
            var customer = await _customerRepo.GetByIdAsync(userData.CustomerId.Value, ct);

            if (customer is not null)
            {
                fullName = customer.FullName;
                customerEmail = customer.Email;
            }
        }

        return Result<ProfileDto>.Success(new ProfileDto
        {
            UserId = userData.UserId,
            CustomerId = userData.CustomerId,
            FullName = fullName,
            Phone = userData.PhoneNumber,
            Email = customerEmail ?? userData.Email,
            IsProfileComplete = userData.CustomerId.HasValue && !string.IsNullOrEmpty(fullName),
            CreatedAt = userData.CreatedAtUtc
        });
    }

    public async Task<Result<ProfileDto>> CompleteProfileAsync(
        Guid userId,
        CompleteProfileRequest request,
        CancellationToken ct = default)
    {
        var userData = await _userReader.GetUserDataAsync(userId, ct);

        if (userData is null)
        {
            return Result<ProfileDto>.Failure(
                "کاربر یافت نشد",
                ErrorCodes.Identity.USER_NOT_FOUND);
        }

        // اگر قبلاً پروفایل دارد، خطا بده
        if (userData.CustomerId.HasValue)
        {
            var existingCustomer = await _customerRepo.ExistsAsync(userData.CustomerId.Value, ct);

            if (existingCustomer)
            {
                return Result<ProfileDto>.Failure(
                    "پروفایل قبلاً تکمیل شده است",
                    ErrorCodes.Profile.PROFILE_ALREADY_COMPLETE);
            }
        }

        // ساخت Customer جدید
        Guid customerId;
        try
        {
            customerId = await _customerRepo.CreateAsync(new CreateCustomerData
            {
                UserId = userId,
                FullName = request.FullName,
                Email = request.Email,
                Phone = userData.PhoneNumber
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Customer for User {UserId}", userId);
            return Result<ProfileDto>.Failure(
                "خطا در ایجاد پروفایل",
                ErrorCodes.Profile.CUSTOMER_CREATION_FAILED);
        }

        // آپدیت User.CustomerId
        var updated = await _userReader.SetCustomerIdAsync(userId, customerId, ct);

        if (!updated)
        {
            _logger.LogError(
                "Failed to set CustomerId {CustomerId} for User {UserId}",
                customerId, userId);
            // Customer ساخته شده ولی User آپدیت نشده - این یک inconsistency است
            // در production باید compensation داشته باشیم
        }

        _logger.LogInformation(
            "Profile completed for User {UserId}, Customer {CustomerId} created",
            userId, customerId);

        return Result<ProfileDto>.Success(new ProfileDto
        {
            UserId = userId,
            CustomerId = customerId,
            FullName = request.FullName,
            Phone = userData.PhoneNumber,
            Email = request.Email ?? userData.Email,
            IsProfileComplete = true,
            CreatedAt = userData.CreatedAtUtc
        });
    }

    public async Task<Result<ProfileDto>> UpdateProfileAsync(
        Guid userId,
        CompleteProfileRequest request,
        CancellationToken ct = default)
    {
        var userData = await _userReader.GetUserDataAsync(userId, ct);

        if (userData is null)
        {
            return Result<ProfileDto>.Failure(
                "کاربر یافت نشد",
                ErrorCodes.Identity.USER_NOT_FOUND);
        }

        if (!userData.CustomerId.HasValue)
        {
            return Result<ProfileDto>.Failure(
                "ابتدا پروفایل را تکمیل کنید",
                ErrorCodes.Profile.PROFILE_INCOMPLETE);
        }

        var customer = await _customerRepo.GetByIdAsync(userData.CustomerId.Value, ct);

        if (customer is null)
        {
            return Result<ProfileDto>.Failure(
                "پروفایل یافت نشد",
                ErrorCodes.Profile.PROFILE_NOT_FOUND);
        }

        // بروزرسانی
        var updated = await _customerRepo.UpdateAsync(
            userData.CustomerId.Value,
            new UpdateCustomerData
            {
                FullName = request.FullName,
                Email = request.Email
            }, ct);

        if (!updated)
        {
            return Result<ProfileDto>.Failure(
                "خطا در بروزرسانی پروفایل",
                ErrorCodes.Profile.CUSTOMER_UPDATE_FAILED);
        }

        _logger.LogInformation(
            "Profile updated for User {UserId}, Customer {CustomerId}",
            userId, userData.CustomerId);

        return Result<ProfileDto>.Success(new ProfileDto
        {
            UserId = userId,
            CustomerId = userData.CustomerId,
            FullName = request.FullName,
            Phone = userData.PhoneNumber,
            Email = request.Email ?? userData.Email,
            IsProfileComplete = true,
            CreatedAt = userData.CreatedAtUtc
        });
    }
}
