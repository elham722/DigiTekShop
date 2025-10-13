namespace DigiTekShop.API.Controllers.Customers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/customers")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
[Consumes("application/json")]
[ApiExplorerSettings(GroupName = "v1-customers")]
[Tags("Customers")]
public sealed class CustomersCommandController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<CustomersCommandController> _logger;

    public CustomersCommandController(ISender sender, ILogger<CustomersCommandController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    
    [HttpPost("{customerId:guid}/addresses")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAddress(
        [FromRoute] Guid customerId,
        [FromBody] AddAddressRequest request,
        [FromQuery] bool asDefault = false,
        CancellationToken ct = default)
    {
        
        var addressDto = new AddressDto(
            request.Line1,
            request.Line2,
            request.City,
            request.State,
            request.PostalCode,
            request.Country,
            request.IsDefault
        );
        
        var result = await _sender.Send(new AddAddressCommand(customerId, addressDto, asDefault), ct);
        if (!result.IsSuccess) return this.ToActionResult(result);

        _logger.LogInformation("Address added for customer: {CustomerId}", customerId);
        return this.ToActionResult(result);
    }

    /// <summary>Change customer email</summary>
    [HttpPut("{customerId:guid}/email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEmail(
        [FromRoute] Guid customerId,
        [FromBody] ChangeEmailRequest request,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new ChangeEmailCommand(customerId, request.NewEmail), ct);
        if (!result.IsSuccess) return this.ToActionResult(result);

        _logger.LogInformation("Email changed for customer: {CustomerId}", customerId);
        return this.ToActionResult(result);
    }

    /// <summary>Set default address for customer</summary>
    [HttpPut("{customerId:guid}/addresses/{addressIndex:int}/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultAddress(
        [FromRoute] Guid customerId,
        [FromRoute] int addressIndex,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new SetDefaultAddressCommand(customerId, addressIndex), ct);
        if (!result.IsSuccess) return this.ToActionResult(result);

        _logger.LogInformation("Default address set: {CustomerId} idx={Index}", customerId, addressIndex);
        return this.ToActionResult(result);
    }

    /// <summary>Update customer profile</summary>
    [HttpPut("{customerId:guid}/profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromRoute] Guid customerId,
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new UpdateProfileCommand(customerId, request.FullName, request.Phone), ct);
        if (!result.IsSuccess) return this.ToActionResult(result);

        _logger.LogInformation("Profile updated: {CustomerId}", customerId);
        return this.ToActionResult(result);
    }
}

