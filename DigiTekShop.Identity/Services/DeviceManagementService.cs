using DigiTekShop.Contracts.DTOs.Auth.UserDevice;
using DigiTekShop.Contracts.Interfaces.Identity.Auth;
using DigiTekShop.Identity.Models;
using DigiTekShop.Identity.Options;
using DigiTekShop.SharedKernel.Exceptions.Common;
using DigiTekShop.SharedKernel.Exceptions.NotFound;
using DigiTekShop.SharedKernel.Exceptions.Validation;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigiTekShop.Identity.Services;


public class DeviceManagementService : IDeviceManagementService
{
    private readonly UserManager<User> _userManager;
    private readonly DigiTekShopIdentityDbContext _context;
    private readonly DeviceLimitsSettings _deviceLimits;
    private readonly ILogger<DeviceManagementService> _logger;

    public DeviceManagementService(
        UserManager<User> userManager,
        DigiTekShopIdentityDbContext context,
        IOptions<DeviceLimitsSettings> deviceLimitsOptions,
        ILogger<DeviceManagementService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _deviceLimits = deviceLimitsOptions?.Value ?? throw new ArgumentNullException(nameof(deviceLimitsOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IEnumerable<UserDeviceDto>>> GetUserDevicesAsync(
        string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<IEnumerable<UserDeviceDto>>.Failure("User ID is required");

      
        if (!Guid.TryParse(userId, out _))
            return Result<IEnumerable<UserDeviceDto>>.Failure("Invalid user ID format");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result<IEnumerable<UserDeviceDto>>.Failure("User not found");

        var dtos = await _context.UserDevices
            .Where(d => d.UserId == user.Id) 
            .OrderByDescending(d => d.LastLoginAt)
            .Select(d => new UserDeviceDto
            {
                DeviceId = d.Id,
                DeviceName = d.DeviceName,
                Platform = d.DeviceFingerprint,
                IpAddress = d.IpAddress,
                UserAgent = d.BrowserInfo,
                IsTrusted = d.IsTrusted,
                LastLoginAt = d.LastLoginAt
            })
            .ToListAsync(ct);

        return Result<IEnumerable<UserDeviceDto>>.Success(dtos);
    }


    public async Task<Result> TrustDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure("User ID is required");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure("User not found");

        try
        {
            user.TrustDevice(deviceId, _deviceLimits.MaxTrustedDevicesPerUser);
            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Device {DeviceId} trusted for user {UserId}", deviceId, userId);
            return Result.Success();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Device {DeviceId} not found for user {UserId}", deviceId, userId);
            return Result.Failure(ex.Message);
        }
        catch (InvalidDomainOperationException ex)
        {
            _logger.LogWarning("Cannot trust device {DeviceId} for user {UserId}: {Message}", deviceId, userId, ex.Message);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> UntrustDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure("User ID is required");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure("User not found");

        try
        {
            user.UntrustDevice(deviceId);
            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Device {DeviceId} untrusted for user {UserId}", deviceId, userId);
            return Result.Success();
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Device {DeviceId} not found for user {UserId}", deviceId, userId);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> RemoveDeviceAsync(string userId, Guid deviceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure("User ID is required");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure("User not found");

        try
        {
            user.RemoveDevice(deviceId);
            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Device {DeviceId} removed for user {UserId}", deviceId, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing device {DeviceId} for user {UserId}", deviceId, userId);
            return Result.Failure("Failed to remove device");
        }
    }

    public async Task<Result> CleanupInactiveDevicesAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure("User ID is required");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure("User not found");

        try
        {
           
            user.DeactivateInactiveDevices(_deviceLimits.DeviceInactivityThreshold);
            
            
            var removalThreshold = TimeSpan.FromDays(90);
            user.RemoveOldInactiveDevices(removalThreshold);
            
            await _context.SaveChangesAsync(ct);
            
            _logger.LogInformation("Device cleanup completed for user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during device cleanup for user {UserId}", userId);
            return Result.Failure("Failed to cleanup devices");
        }
    }

    public async Task<Result<DeviceStatsDto>> GetDeviceStatsAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<DeviceStatsDto>.Failure("User ID is required");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<DeviceStatsDto>.Failure("User not found");

        var stats = new DeviceStatsDto
        {
            TotalDevices = user.Devices.Count,
            ActiveDevices = user.GetActiveDeviceCount(),
            TrustedDevices = user.GetTrustedDeviceCount(),
            MaxActiveDevices = _deviceLimits.MaxActiveDevicesPerUser,
            MaxTrustedDevices = _deviceLimits.MaxTrustedDevicesPerUser,
            LastCleanupAt = DateTime.UtcNow 
        };

        return Result<DeviceStatsDto>.Success(stats);
    }
}

