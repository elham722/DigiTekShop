using DigiTekShop.API.Common.Api;
using DigiTekShop.API.ResultMapping;
using DigiTekShop.Application.Admin.Users.Commands.LockUser;
using DigiTekShop.Application.Admin.Users.Commands.UnlockUser;
using DigiTekShop.Application.Admin.Users.Queries.GetAdminUserDetails;
using DigiTekShop.Application.Admin.Users.Queries.GetAdminUserList;
using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.Abstractions.Search;
using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Contracts.DTOs.Auth.Lockout;

namespace DigiTekShop.API.Controllers.Admin.V1;

[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Tags("Admin - Users")]
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

   
    [HttpPost("reindex-search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReindexUsers(CancellationToken ct)
    {
        var result = await _userSearchIndexingService.ReindexAllUsersAsync(ct);
        return this.ToActionResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdminUserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserListQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminUserListQuery(query), ct);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetails(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminUserDetailsQuery(id), ct);
        return this.ToActionResult(result);
    }

    [HttpPost("{id:guid}/lock")]
    [ProducesResponseType(typeof(ApiResponse<LockUserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LockUser(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new LockUserCommand(id), ct);
        return this.ToActionResult(result);
    }

    [HttpPost("{id:guid}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<UnlockUserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new UnlockUserCommand(id), ct);
        return this.ToActionResult(result);
    }
}
