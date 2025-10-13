namespace DigiTekShop.API.Controllers.Customers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/customers")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "v1-customers")]
[Tags("Customers")]
public sealed class CustomersQueryController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<CustomersQueryController> _logger;

    public CustomersQueryController(ISender sender, ILogger<CustomersQueryController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    [HttpGet("{customerId:guid}", Name = "GetCustomerById")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetCustomerById([FromRoute] Guid customerId, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetCustomerByIdQuery(customerId), ct);

        if (result.IsSuccess && result.Value is null)
        {
            return NotFound();
        }

        if (result.IsSuccess && result.Value is not null)
        {
            // Convert to response DTO
            var customer = result.Value;
            var response = new CustomerResponse(
                customer.Id,
                customer.UserId,
                customer.FullName,
                customer.Email,
                customer.Phone,
                DateTime.UtcNow, // TODO: Add CreatedAt to domain
                DateTime.UtcNow, // TODO: Add UpdatedAt to domain
                customer.Addresses.Select(a => new AddressResponse(
                    a.Line1,
                    a.Line2,
                    a.City,
                    a.State,
                    a.PostalCode,
                    a.Country,
                    a.IsDefault
                )).ToList()
            );

            // ETag support
            var etag = response.GenerateETag();
            var notModified = HttpContext.CheckETag(etag);
            if (notModified != null) return notModified;

            HttpContext.Response.SetETag(etag);
            return this.ToActionResult(result);
        }

        return this.ToActionResult(result);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<CustomerView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid or missing NameIdentifier");
            return Unauthorized();
        }

        var result = await _sender.Send(new GetMyCustomerProfileQuery(userId), ct);

        if (result.IsSuccess && result.Value is null)
            return NotFound();

        return this.ToActionResult(result);
    }
}
