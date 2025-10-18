using DigiTekShop.SharedKernel.Enums.Auth;

namespace DigiTekShop.Contracts.DTOs.Auth.Register
{
    public record RegisterResponseDto(
        Guid UserId,
        bool RequireEmailConfirmation,
        bool EmailSent,
        bool RequirePhoneConfirmation,
        bool PhoneCodeSent,
        RegisterNextStep NextStep = RegisterNextStep.None
    );
}