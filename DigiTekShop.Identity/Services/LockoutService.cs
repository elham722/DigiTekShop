using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Models;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using DigiTekShop.Contracts.Auth.Lockout;

namespace DigiTekShop.Identity.Services;

public sealed class LockoutService : ILockoutService
{
    private readonly UserManager<User> _userManager;
    private readonly IValidator<LockUserRequestDto> _lockUserValidator;
    private readonly IValidator<UnlockUserRequestDto> _unlockUserValidator;

    public LockoutService(
        UserManager<User> userManager,
        IValidator<LockUserRequestDto> lockUserValidator,
        IValidator<UnlockUserRequestDto> unlockUserValidator)
    {
        _userManager = userManager;
        _lockUserValidator = lockUserValidator;
        _unlockUserValidator = unlockUserValidator;
    }

    public async Task<Result<LockUserResponseDto>> LockUserAsync(LockUserRequestDto req, CancellationToken ct = default)
    {
        // ✅ Validate با FluentValidation
        var validationResult = await _lockUserValidator.ValidateAsync(req, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<LockUserResponseDto>.Failure(errors);
        }

        if (!Guid.TryParse(req.UserId.ToString(), out var uid))
            return Result<LockUserResponseDto>.Failure("Invalid user id");

        var user = await _userManager.FindByIdAsync(req.UserId.ToString());
        if (user is null) return Result<LockUserResponseDto>.Failure("User not found");

        var prevEnd = await _userManager.GetLockoutEndDateAsync(user);

        if (!await _userManager.GetLockoutEnabledAsync(user))
            await _userManager.SetLockoutEnabledAsync(user, true);


        var end = req.LockoutEnd ?? DateTimeOffset.UtcNow.AddMinutes(15); 
        var setRes = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!setRes.Succeeded)
            return Result<LockUserResponseDto>.Failure(setRes.Errors.Select(e => e.Description));

        var dto = new LockUserResponseDto(uid, true, end, prevEnd, "User locked");
        return Result<LockUserResponseDto>.Success(dto);
    }

    public async Task<Result<UnlockUserResponseDto>> UnlockUserAsync(UnlockUserRequestDto req, CancellationToken ct = default)
    {
        // ✅ Validate با FluentValidation
        var validationResult = await _unlockUserValidator.ValidateAsync(req, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<UnlockUserResponseDto>.Failure(errors);
        }

        if (!Guid.TryParse(req.UserId.ToString(), out var uid))
            return Result<UnlockUserResponseDto>.Failure("Invalid user id");

        var user = await _userManager.FindByIdAsync(req.UserId.ToString());
        if (user is null) return Result<UnlockUserResponseDto>.Failure("User not found");

       
        var setRes = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
        if (!setRes.Succeeded)
            return Result<UnlockUserResponseDto>.Failure(setRes.Errors.Select(e => e.Description));

      
        await _userManager.ResetAccessFailedCountAsync(user);

        var end = await _userManager.GetLockoutEndDateAsync(user);
        var dto = new UnlockUserResponseDto(uid, false, end, "User unlocked");
        return Result<UnlockUserResponseDto>.Success(dto);
    }

    public async Task<Result<LockoutStatusResponseDto>> GetLockoutStatusAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Result<LockoutStatusResponseDto>.Failure("User not found");

        var end = await _userManager.GetLockoutEndDateAsync(user);
        var isLocked = end.HasValue && end.Value > DateTimeOffset.UtcNow;
        return Result<LockoutStatusResponseDto>.Success(new LockoutStatusResponseDto(isLocked, end));
    }

    public async Task<Result<TimeSpan?>> GetLockoutEndTimeAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Result<TimeSpan?>.Failure("User not found");

        var end = await _userManager.GetLockoutEndDateAsync(user);
        if (!end.HasValue || end.Value <= DateTimeOffset.UtcNow)
            return Result<TimeSpan?>.Success(null);

        return Result<TimeSpan?>.Success(end.Value - DateTimeOffset.UtcNow);
    }
}
