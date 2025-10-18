using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;

namespace DigiTekShop.Application.Auth.ResendEmailConfirmation.Command;
public sealed record ResendEmailConfirmationCommand(ResendEmailConfirmationRequestDto Dto) : ICommand, INonTransactionalCommand;
