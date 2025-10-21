using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Mfa;

namespace DigiTekShop.Application.Auth.Mfa.Command;
public sealed record VerifyMfaCommand(VerifyMfaRequest Dto)
    : ICommand<LoginResponse>, INonTransactionalCommand;