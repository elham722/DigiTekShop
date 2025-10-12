using Asp.Versioning;
using DigiTekShop.API.Controllers.Common.V1;
using DigiTekShop.API.Extensions;
using DigiTekShop.API.Models;
using DigiTekShop.Application.Customers.Commands.AddAddress;
using DigiTekShop.Application.Customers.Commands.ChangeEmail;
using DigiTekShop.Application.Customers.Commands.RegisterCustomer;
using DigiTekShop.Application.Customers.Commands.SetDefaultAddress;
using DigiTekShop.Application.Customers.Commands.UpdateProfile;
using DigiTekShop.Contracts.DTOs.Customer;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiTekShop.API.Controllers.Customers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/customers")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
[Consumes("application/json")]
[ApiExplorerSettings(GroupName = "v1-customers")]
[Tags("Customers")]
public sealed class CustomersCommandController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<CustomersCommandController> _logger;

    public CustomersCommandController(ISender sender, ILogger<CustomersCommandController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>Register a new customer</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerDto request, CancellationToken ct)
    {
        var result = await _sender.Send(new RegisterCustomerCommand(request), ct);
         return this.ToActionResult(result);

    }

    /// <summary>Add a new address to customer</summary>
    [HttpPost("{customerId:guid}/addresses")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAddress(
        [FromRoute] Guid customerId,
        [FromBody] AddressDto request,
        [FromQuery] bool asDefault = false,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new AddAddressCommand(customerId, request, asDefault), ct);
        if (!result.IsSuccess) return this.ToActionResult(result);

        _logger.LogInformation("Address added for customer: {CustomerId}", customerId);

        // اگر آدرس id ندارد، مسیر مشتری را برگردان
        return CreatedAtAction(
            nameof(CustomersQueryController.GetCustomerById),
            "CustomersQuery",
            new { version = "1.0", customerId },
            null);
    }

    
    [HttpPut("{customerId:guid}/email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEmail(ChangeEmailCommand command,CancellationToken ct = default)
    {
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess) return this.ToActionResult(result);

        _logger.LogInformation("Email changed for customer: {CustomerId}", command.CustomerId);
        return NoContent();
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
        return NoContent();
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
        return NoContent();
    }
}

public sealed record UpdateProfileRequest(string FullName, string? Phone);

