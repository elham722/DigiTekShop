using DigiTekShop.Contracts.DTOs.Auth.Token;

namespace DigiTekShop.Application.Auth.Tokens.Command;
public sealed record RefreshTokenCommand(RefreshTokenRequest Dto)
    : ICommand<RefreshTokenResponse>, INonTransactionalCommand;