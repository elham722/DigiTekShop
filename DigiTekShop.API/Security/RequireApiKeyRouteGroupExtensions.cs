using Microsoft.AspNetCore.Routing;

namespace DigiTekShop.API.Security;

public static class RequireApiKeyRouteGroupExtensions
{
    public static RouteGroupBuilder RequireApiKey(this RouteGroupBuilder group)
    {
        group.WithMetadata(new RequireApiKeyAttribute());
        return group;
    }
}