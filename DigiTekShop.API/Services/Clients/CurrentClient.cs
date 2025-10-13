using DigiTekShop.Contracts.Abstractions.Clients;

namespace DigiTekShop.API.Services.Clients
{
    public sealed class CurrentClient(IHttpContextAccessor accessor) : ICurrentClient
    {
        public string? DeviceId => TryGet("DeviceId");
        public string? UserAgent => TryGet("UserAgent");
        public string? IpAddress => TryGet("IpAddress");

        private string? TryGet(string key)
            => accessor.HttpContext?.Items.TryGetValue(key, out var v) == true ? v as string : null;
    }
}
