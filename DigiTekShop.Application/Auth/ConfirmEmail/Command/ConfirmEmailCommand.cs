using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;

namespace DigiTekShop.Application.Auth.ConfirmEmail.Command;
public sealed record ConfirmEmailCommand(ConfirmEmailRequestDto Dto) : ICommand, INonTransactionalCommand;
