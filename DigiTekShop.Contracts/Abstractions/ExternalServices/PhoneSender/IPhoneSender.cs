namespace DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;

public interface IPhoneSender
{
    Task<Result> SendCodeAsync(string phoneNumber, string code, string? templateName = null);
}
