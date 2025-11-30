using DigiTekShop.Contracts.Abstractions.Repositories.Customers;

namespace DigiTekShop.Application.Profile.Commands.UpdateMyProfile;

/// <summary>
/// هندلر آپدیت پروفایل کاربر
/// </summary>
public sealed class UpdateMyProfileCommandHandler : ICommandHandler<UpdateMyProfileCommand>
{
    private readonly ICustomerCommandRepository _customerCommand;

    public UpdateMyProfileCommandHandler(ICustomerCommandRepository customerCommand)
    {
        _customerCommand = customerCommand;
    }

    public async Task<Result> Handle(UpdateMyProfileCommand request, CancellationToken ct)
    {
        return await _customerCommand.UpdateProfileAsync(
            request.UserId,
            request.FullName,
            request.Email,
            request.Phone,
            ct);
    }
}

