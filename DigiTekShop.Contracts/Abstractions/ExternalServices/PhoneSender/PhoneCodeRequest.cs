namespace DigiTekShop.Contracts.Abstractions.ExternalServices.PhoneSender;

public record PhoneCodeRequest(
    string PhoneNumber,
    string Code,
    string? TemplateName = null
);
