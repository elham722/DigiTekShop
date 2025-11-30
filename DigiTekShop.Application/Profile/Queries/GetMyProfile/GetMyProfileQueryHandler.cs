using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Contracts.DTOs.Profile;
using DigiTekShop.SharedKernel.Errors;

namespace DigiTekShop.Application.Profile.Queries.GetMyProfile;

/// <summary>
/// هندلر دریافت پروفایل کاربر
/// </summary>
public sealed class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, MyProfileDto>
{
    private readonly ICustomerQueryRepository _customerQuery;

    public GetMyProfileQueryHandler(ICustomerQueryRepository customerQuery)
    {
        _customerQuery = customerQuery;
    }

    public async Task<Result<MyProfileDto>> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var profile = await _customerQuery.GetProfileByUserIdAsync(request.UserId, ct);

        if (profile is null)
        {
            return Result<MyProfileDto>.Failure(
                "پروفایل کاربر یافت نشد",
                ErrorCodes.Profile.PROFILE_NOT_FOUND);
        }

        return Result<MyProfileDto>.Success(profile);
    }
}

