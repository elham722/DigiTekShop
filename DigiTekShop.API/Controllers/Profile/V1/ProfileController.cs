using DigiTekShop.Application.Profile.Commands.CompleteProfile;
using DigiTekShop.Application.Profile.Commands.UpdateProfile;
using DigiTekShop.Application.Profile.Queries.GetProfile;
using DigiTekShop.Application.Profile.Queries.GetProfileStatus;
using DigiTekShop.Contracts.DTOs.Profile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Controllers.Profile.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// دریافت پروفایل کاربر جاری
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetProfileQuery(userId.Value), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return NotFound(new { error = result.ErrorCode, message = result.GetFirstError() });
    }

    /// <summary>
    /// دریافت وضعیت تکمیل پروفایل
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ProfileCompletionStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfileStatus(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetProfileStatusQuery(userId.Value), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return NotFound(new { error = result.ErrorCode, message = result.GetFirstError() });
    }

    /// <summary>
    /// تکمیل پروفایل (ساخت Customer)
    /// </summary>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteProfile(
        [FromBody] CompleteProfileRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(
            new CompleteProfileCommand(userId.Value, request), ct);

        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetProfile), result.Value);

        return result.ErrorCode switch
        {
            "PROFILE_ALREADY_COMPLETE" => Conflict(new { error = result.ErrorCode, message = result.GetFirstError() }),
            "VALIDATION_FAILED" => BadRequest(new { error = result.ErrorCode, message = result.GetFirstError() }),
            _ => BadRequest(new { error = result.ErrorCode, message = result.GetFirstError() })
        };
    }

    /// <summary>
    /// بروزرسانی پروفایل
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] CompleteProfileRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(
            new UpdateProfileCommand(userId.Value, request), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode switch
        {
            "PROFILE_INCOMPLETE" => BadRequest(new { error = result.ErrorCode, message = "ابتدا پروفایل را تکمیل کنید" }),
            "PROFILE_NOT_FOUND" => NotFound(new { error = result.ErrorCode, message = result.GetFirstError() }),
            _ => BadRequest(new { error = result.ErrorCode, message = result.GetFirstError() })
        };
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim is null || !Guid.TryParse(claim.Value, out var userId))
            return null;

        return userId;
    }
}

