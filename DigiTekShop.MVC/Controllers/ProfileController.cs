using DigiTekShop.Contracts.Abstractions.Profile;
using DigiTekShop.Contracts.DTOs.Profile;
using DigiTekShop.MVC.Filters;
using DigiTekShop.MVC.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.MVC.Controllers;

[Authorize]
public sealed class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// صفحه پروفایل کاربر
    /// </summary>
    [HttpGet]
    [RequireCompleteProfile]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Auth");

        var result = await _profileService.GetProfileAsync(userId.Value, ct);

        if (result.IsFailure)
        {
            TempData["Error"] = "خطا در دریافت اطلاعات پروفایل";
            return RedirectToAction("Index", "Home");
        }

        var viewModel = new ProfileViewModel
        {
            FullName = result.Value.FullName ?? "",
            Email = result.Value.Email ?? "",
            Phone = result.Value.Phone ?? "",
            IsProfileComplete = result.Value.IsProfileComplete,
            CreatedAt = result.Value.CreatedAt
        };

        return View(viewModel);
    }

    /// <summary>
    /// صفحه تکمیل پروفایل
    /// </summary>
    [HttpGet]
    [SkipProfileCheck]
    public async Task<IActionResult> Complete(string? returnUrl, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Auth");

        // چک کنیم که آیا واقعاً پروفایل ناقص است
        var statusResult = await _profileService.GetCompletionStatusAsync(userId.Value, ct);

        if (statusResult.IsSuccess && statusResult.Value.IsComplete)
        {
            // پروفایل قبلاً کامل شده، redirect کن
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        // گرفتن اطلاعات فعلی (اگر وجود دارد)
        var profileResult = await _profileService.GetProfileAsync(userId.Value, ct);

        var viewModel = new CompleteProfileViewModel
        {
            FullName = profileResult.IsSuccess ? profileResult.Value.FullName ?? "" : "",
            Email = profileResult.IsSuccess ? profileResult.Value.Email ?? "" : "",
            Phone = profileResult.IsSuccess ? profileResult.Value.Phone ?? "" : "",
            ReturnUrl = returnUrl
        };

        return View(viewModel);
    }

    /// <summary>
    /// ثبت تکمیل پروفایل
    /// </summary>
    [HttpPost]
    [SkipProfileCheck]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CompleteProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Auth");

        var request = new CompleteProfileRequest
        {
            FullName = model.FullName,
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email
        };

        var result = await _profileService.CompleteProfileAsync(userId.Value, request, ct);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to complete profile for user {UserId}: {Error}",
                userId, result.ErrorCode);

            ModelState.AddModelError("", result.GetFirstError() ?? "خطا در تکمیل پروفایل");
            return View(model);
        }

        _logger.LogInformation("Profile completed for user {UserId}", userId);
        TempData["Success"] = "پروفایل شما با موفقیت تکمیل شد!";

        // Redirect به صفحه درخواستی یا صفحه اصلی
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// صفحه ویرایش پروفایل
    /// </summary>
    [HttpGet]
    [RequireCompleteProfile]
    public async Task<IActionResult> Edit(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Auth");

        var result = await _profileService.GetProfileAsync(userId.Value, ct);

        if (result.IsFailure)
        {
            TempData["Error"] = "خطا در دریافت اطلاعات پروفایل";
            return RedirectToAction("Index");
        }

        var viewModel = new EditProfileViewModel
        {
            FullName = result.Value.FullName ?? "",
            Email = result.Value.Email ?? ""
        };

        return View(viewModel);
    }

    /// <summary>
    /// ثبت ویرایش پروفایل
    /// </summary>
    [HttpPost]
    [RequireCompleteProfile]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetUserId();
        if (userId is null)
            return RedirectToAction("Login", "Auth");

        var request = new CompleteProfileRequest
        {
            FullName = model.FullName,
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email
        };

        var result = await _profileService.UpdateProfileAsync(userId.Value, request, ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.GetFirstError() ?? "خطا در بروزرسانی پروفایل");
            return View(model);
        }

        TempData["Success"] = "پروفایل شما با موفقیت بروزرسانی شد!";
        return RedirectToAction("Index");
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub")
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim is null || !Guid.TryParse(claim.Value, out var userId))
            return null;

        return userId;
    }
}

