namespace DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;

public interface IPhoneSender
{
    Task<Result> SendCodeAsync(string phoneNumber, string code, string? templateName = null);
    Task<Result> SendBulkCodeAsync(PhoneCodeRequest[] requests);
    Task<Result> TestConnectionAsync();
    Task<Result<double>> GetCreditAsync();
    PhoneSenderSettings Settings { get; }
}
