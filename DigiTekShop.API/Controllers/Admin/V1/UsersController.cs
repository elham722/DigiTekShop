using DigiTekShop.API.Common.Api;
using DigiTekShop.API.ResultMapping;
using DigiTekShop.API.Services.Search;
using DigiTekShop.Application.Admin.Users.Queries.GetAdminUserList;
using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.DTOs.Admin.Users;

namespace DigiTekShop.API.Controllers.Admin.V1;

[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Tags("Admin - Users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly UserSearchIndexingService _userSearchIndexingService;

    public UsersController(
        ISender sender,
        UserSearchIndexingService userSearchIndexingService)
    {
        _sender = sender;
        _userSearchIndexingService = userSearchIndexingService;
    }

    // الان آدرس نهایی میشه:
    // POST /api/v1/admin/users/reindex-search
    [HttpPost("reindex-search")]
    public async Task<IActionResult> ReindexUsers(CancellationToken ct)
    {
        await _userSearchIndexingService.ReindexAllUsersAsync(ct);
        return Ok(new { message = "Reindex started/completed" });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdminUserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserListQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminUserListQuery(query), ct);
        return this.ToActionResult(result);
    }
}
