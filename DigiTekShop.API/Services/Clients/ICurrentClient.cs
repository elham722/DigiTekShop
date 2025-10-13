namespace DigiTekShop.API.Services.Clients
{
    public interface ICurrentClient
    {
        string? DeviceId { get; }
        string? UserAgent { get; }
        string? IpAddress { get; }
    }
}
