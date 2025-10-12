using Asp.Versioning;
using DigiTekShop.API.Controllers.Common.V1;
using DigiTekShop.API.Extensions;
using DigiTekShop.API.Models;
using DigiTekShop.Application.Customers.Queries.GetCustomerById;
using DigiTekShop.Application.Customers.Queries.GetMyCustomerProfile;
using DigiTekShop.Contracts.DTOs.Customer;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DigiTekShop.API.Controllers.Customers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/customers")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public sealed class CustomersQueryController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<CustomersQueryController> _logger;

    public CustomersQueryController(ISender sender, ILogger<CustomersQueryController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

  
    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerById(
        Guid customerId,
        CancellationToken ct = default)
    {
        var query = new GetCustomerByIdQuery(customerId);
        var result = await _sender.Send(query, ct);

        // If customer not found, return 404
        if (result.IsSuccess && result.Value is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Customer not found",
                Detail = $"Customer with ID {customerId} was not found.",
                Instance = HttpContext.Request.Path
            });
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
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("User ID not found in claims or invalid");
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "User ID not found in token claims.",
                Instance = HttpContext.Request.Path
            });
        }

        var query = new GetMyCustomerProfileQuery(userId);
        var result = await _sender.Send(query, ct);

        // If customer profile not found for this user
        if (result.IsSuccess && result.Value is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Customer profile not found",
                Detail = "No customer profile exists for this user. Please register as a customer first.",
                Instance = HttpContext.Request.Path
            });
        }

        return this.ToActionResult(result);
    }
}
