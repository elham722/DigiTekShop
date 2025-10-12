namespace DigiTekShop.Application.Customers.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid CustomerId,
    string FullName,
    string? Phone
) : ICommand;