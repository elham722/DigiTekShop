namespace DigiTekShop.API.Controllers.Users.V1;

[ApiController]
[Route("api/v{version:apiVersion}/customers")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
[Consumes("application/json")]
[ApiExplorerSettings(GroupName = "v1-customers")]
[Tags("Customers")]
public sealed class UsersCommandController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<UsersCommandController> _logger;

    public UsersCommandController(ISender sender, ILogger<UsersCommandController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

}

