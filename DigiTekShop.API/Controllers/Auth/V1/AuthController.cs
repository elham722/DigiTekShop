namespace DigiTekShop.API.Controllers.Auth.V1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ILoginService _loginService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILoginService loginService, ILogger<AuthController> logger)
    {
        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


}
