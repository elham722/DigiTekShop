namespace DigiTekShop.Application.Customers.Commands.ChangeEmail;

public sealed record ChangeEmailCommand(Guid CustomerId, string NewEmail)
    : ICommand;