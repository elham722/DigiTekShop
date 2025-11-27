using DigiTekShop.Contracts.Abstractions.Identity.Admin;
using DigiTekShop.Contracts.DTOs.Admin.Users;

namespace DigiTekShop.Application.Admin.Users.Queries.GetAdminUserDetails;

public sealed class GetAdminUserDetailsQueryHandler
    : IQueryHandler<GetAdminUserDetailsQuery, AdminUserDetailsDto>
{
    private readonly IAdminUserReadService _adminUserReadService;

    public GetAdminUserDetailsQueryHandler(IAdminUserReadService adminUserReadService)
    {
        _adminUserReadService = adminUserReadService;
    }

    public async Task<Result<AdminUserDetailsDto>> Handle(
        GetAdminUserDetailsQuery request,
        CancellationToken ct)
    {
        return await _adminUserReadService.GetUserDetailsAsync(request.UserId, ct);
    }
}

