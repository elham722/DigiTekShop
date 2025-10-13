using DigiTekShop.Contracts.DTOs.Auth.Register;

namespace DigiTekShop.Application.Auth.Register.Command
{
    public sealed record RegisterUserCommand(RegisterRequestDto Dto)
        : ICommand<RegisterResponseDto>;

}
