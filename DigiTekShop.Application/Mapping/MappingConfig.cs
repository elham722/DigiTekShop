using DigiTekShop.Contracts.DTOs.Admin.Users;
using DigiTekShop.Contracts.DTOs.Search;

namespace DigiTekShop.Application.Mapping;

public static class MappingConfig
{
    public static void Register(TypeAdapterConfig cfg)
    {
        // UserSearchDocument → AdminUserListItemDto
        cfg.NewConfig<UserSearchDocument, AdminUserListItemDto>()
            .Map(dest => dest.Id, src => Guid.Parse(src.Id))
            .Map(dest => dest.FullName, src => src.FullName)
            .Map(dest => dest.Phone, src => src.Phone)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.IsPhoneConfirmed, src => src.IsPhoneConfirmed)
            .Map(dest => dest.IsLocked, src => src.IsLocked)
            .Map(dest => dest.CreatedAtUtc, src => src.CreatedAtUtc)
            .Map(dest => dest.LastLoginAtUtc, src => src.LastLoginAtUtc)
            .Map(dest => dest.Roles, src => src.Roles ?? Array.Empty<string>());
    }
}


