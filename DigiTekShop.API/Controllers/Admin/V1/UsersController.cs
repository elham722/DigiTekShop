using DigiTekShop.API.Common.Api;
using DigiTekShop.API.ResultMapping;
using DigiTekShop.Application.Admin.Users.Queries.GetAdminUserList;
using DigiTekShop.Contracts.Abstractions.Paging;
using DigiTekShop.Contracts.DTOs.Admin.Users;

namespace DigiTekShop.API.Controllers.Admin.V1;

[ApiController]
[Route("api/v{version:apiVersion}/admin/[controller]")]
[ApiVersion("1.0")]
//[Authorize(Roles = "")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "v1-admin-users")]
[Tags("Admin - Users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdminUserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserListQuery query, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminUserListQuery(query), ct);
        return this.ToActionResult(result);
    }
}

