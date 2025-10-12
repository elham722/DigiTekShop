namespace DigiTekShop.Contracts.Interfaces.ExternalServices.PhoneSender;

public record PhoneCodeRequest(
    string PhoneNumber,
    string Code,
    string? TemplateName = null
);
