namespace DigiTekShop.API.Controllers.Users.V1;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "v1-users")]
[Tags("Users")]
public sealed class UsersQueryController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<UsersQueryController> _logger;

    public UsersQueryController(ISender sender, ILogger<UsersQueryController> logger)
    {
        _sender = sender;
        _logger = logger;
    }
}
