using DigiTekShop.Contracts.DTOs.Auth.Login;

namespace DigiTekShop.Application.Auth.Mfa.Command;
public sealed record VerifyMfaCommand(VerifyMfaRequest Dto)
    : ICommand<LoginResponse>, INonTransactionalCommand;