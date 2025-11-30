using DigiTekShop.Application.Profile.Commands.UpdateMyProfile;
using DigiTekShop.Application.Profile.Queries.GetMyProfile;
using DigiTekShop.Contracts.DTOs.Profile;
using DigiTekShop.SharedKernel.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Controllers.Profile.V1;

/// <summary>
/// مدیریت پروفایل کاربر
/// </summary>
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
    /// GET /api/v1/profile/me
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(MyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetMyProfileQuery(userId.Value), ct);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorCode switch
        {
            ErrorCodes.Profile.PROFILE_NOT_FOUND => NotFound(new
            {
                error = result.ErrorCode,
                message = result.GetFirstError()
            }),
            _ => BadRequest(new
            {
                error = result.ErrorCode,
                message = result.GetFirstError()
            })
        };
    }

    /// <summary>
    /// بروزرسانی پروفایل کاربر
    /// PUT /api/v1/profile/me
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var command = new UpdateMyProfileCommand(
            userId.Value,
            request.FullName,
            request.Email,
            request.Phone);

        var result = await _mediator.Send(command, ct);

        if (result.IsSuccess)
            return NoContent();

        return result.ErrorCode switch
        {
            ErrorCodes.Profile.PROFILE_NOT_FOUND => NotFound(new
            {
                error = result.ErrorCode,
                message = result.GetFirstError()
            }),
            ErrorCodes.Common.VALIDATION_FAILED => BadRequest(new
            {
                error = result.ErrorCode,
                message = result.GetFirstError()
            }),
            _ => BadRequest(new
            {
                error = result.ErrorCode,
                message = result.GetFirstError()
            })
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
