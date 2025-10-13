namespace DigiTekShop.Contracts.Abstractions.Clients
{
    public interface ICurrentClient
    {
        string? DeviceId { get; }
        string? UserAgent { get; }
        string? IpAddress { get; }
    }
}
