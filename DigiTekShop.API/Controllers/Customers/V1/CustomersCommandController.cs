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
public sealed class CustomersCommandController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<CustomersCommandController> _logger;

    public CustomersCommandController(ISender sender, ILogger<CustomersCommandController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Register a new customer
    /// </summary>
    /// <param name="request">Customer registration data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Customer ID</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterCustomer(
        [FromBody] RegisterCustomerDto request,
        CancellationToken ct)
    {
        var command = new RegisterCustomerCommand(request);
        var result = await _sender.Send(command, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Customer registered successfully: {CustomerId}", result.Value);
            return this.ToActionResult(result, StatusCodes.Status201Created);
        }

        return this.ToActionResult(result);
    }

    /// <summary>
    /// Add a new address to customer
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="request">Address data</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPost("{customerId:guid}/addresses")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAddress(
        Guid customerId,
        [FromBody] AddressDto request,
        [FromQuery] bool asDefault = false,
        CancellationToken ct = default)
    {
        var command = new AddAddressCommand(customerId, request, asDefault);
        var result = await _sender.Send(command, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Address added for customer: {CustomerId}", customerId);
        }

        return this.ToActionResult(result);
    }

    /// <summary>
    /// Change customer email
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="newEmail">New email address</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPut("{customerId:guid}/email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEmail(
        Guid customerId,
        [FromBody] string newEmail,
        CancellationToken ct = default)
    {
        var command = new ChangeEmailCommand(customerId, newEmail);
        var result = await _sender.Send(command, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Email changed for customer: {CustomerId}", customerId);
        }

        return this.ToActionResult(result);
    }

    /// <summary>
    /// Set default address for customer
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="addressIndex">Index of the address to set as default</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPut("{customerId:guid}/addresses/{addressIndex:int}/set-default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultAddress(
        Guid customerId,
        int addressIndex,
        CancellationToken ct = default)
    {
        var command = new SetDefaultAddressCommand(customerId, addressIndex);
        var result = await _sender.Send(command, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Default address set for customer: {CustomerId}, Index: {Index}",
                customerId, addressIndex);
        }

        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update customer profile
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="fullName">Full name</param>
    /// <param name="phone">Phone number (optional)</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPut("{customerId:guid}/profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid customerId,
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        var command = new UpdateProfileCommand(customerId, request.FullName, request.Phone);
        var result = await _sender.Send(command, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Profile updated for customer: {CustomerId}", customerId);
        }

        return this.ToActionResult(result);
    }
}

/// <summary>
/// Request model for updating customer profile
/// </summary>
public sealed record UpdateProfileRequest(string FullName, string? Phone);
