using DigiTekShop.Contracts.DTOs.Admin.Users;

namespace DigiTekShop.Application.Admin.Users.Queries.GetAdminUserDetails;

public sealed record GetAdminUserDetailsQuery(Guid UserId)
    : IQuery<AdminUserDetailsDto>;

