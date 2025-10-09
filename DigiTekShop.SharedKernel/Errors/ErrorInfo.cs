namespace DigiTekShop.SharedKernel.Errors;

public sealed record ErrorInfo(
    string Code,
    int HttpStatus,
    string DefaultMessage
);