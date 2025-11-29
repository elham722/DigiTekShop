using DigiTekShop.API.Common.Api;
using DigiTekShop.API.ResultMapping;
using DigiTekShop.Application.Admin.Users.Commands.LockUser;
using DigiTekShop.Application.Admin.Users.Commands.UnlockUser;
using DigiTekShop.Application.Admin.Users.Queries.GetAdminUserDetails;
using DigiTekShop.Application.Admin.Users.Queries.GetAdminUserList;
using DigiTekShop.Application.Authorization;
using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Contracts.DTOs.Auth.Lockout;
using DigiTekShop.SharedKernel.Authorization;

namespace DigiTekShop.API.Controllers.Admin.V1;

/// <summary>
/// Admin endpoints for user management.
/// All endpoints require specific permissions.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Tags("Admin - Users")]
[Authorize] // Base authentication required
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IUserSearchIndexingService _userSearchIndexingService;

    public UsersController(
        ISender sender,
        IUserSearchIndexingService userSearchIndexingService)
    {
        _sender = sender;
        _userSearchIndexingService = userSearchIndexingService;
    }

    /// <summary>
    /// Reindex all users in Elasticsearch (Admin only)
    /// </summary>
    [HttpPost("reindex-search")]
    [RequirePermission(Permissions.Admin.UsersManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReindexUsers(CancellationToken ct)
    {
        var result = await _userSearchIndexingService.ReindexAllUsersAsync(ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.Admin.UsersView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdminUserListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserListQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminUserListQuery(query), ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Get user details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.Admin.UsersView)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserDetails(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminUserDetailsQuery(id), ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Lock a user account
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    [RequirePermission(Permissions.Admin.UsersLock)]
    [ProducesResponseType(typeof(ApiResponse<LockUserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockUser(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new LockUserCommand(id), ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Unlock a user account
    /// </summary>
    [HttpPost("{id:guid}/unlock")]
    [RequirePermission(Permissions.Admin.UsersLock)]
    [ProducesResponseType(typeof(ApiResponse<UnlockUserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new UnlockUserCommand(id), ct);
        return this.ToActionResult(result);
    }
}
