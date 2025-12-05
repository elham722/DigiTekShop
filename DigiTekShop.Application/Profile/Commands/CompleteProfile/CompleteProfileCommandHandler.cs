using DigiTekShop.Contracts.Abstractions.Identity.Profile;
using DigiTekShop.Contracts.Abstractions.Identity.Token;
using DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;
using DigiTekShop.Contracts.Abstractions.Repositories.Customers;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.SharedKernel.Errors;

namespace DigiTekShop.Application.Profile.Commands.CompleteProfile;

public sealed class CompleteProfileCommandHandler : ICommandHandler<CompleteProfileCommand, RefreshTokenResponse>
{
    private readonly ICustomerCommandRepository _customerCommand;
    private readonly ICustomerQueryRepository _customerQuery;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserProfileService _userProfile;
    private readonly ITokenService _tokenService;

    public CompleteProfileCommandHandler(
        ICustomerCommandRepository customerCommand,
        ICustomerQueryRepository customerQuery,
        IUnitOfWork unitOfWork,
        IUserProfileService userProfile,
        ITokenService tokenService)
    {
        _customerCommand = customerCommand;
        _customerQuery = customerQuery;
        _unitOfWork = unitOfWork;
        _userProfile = userProfile;
        _tokenService = tokenService;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(CompleteProfileCommand request, CancellationToken ct)
    {
        // 1. چک کنیم آیا User قبلاً CustomerId دارد یا نه
        var userHasProfile = await _userProfile.HasProfileAsync(request.UserId, ct);
        
        if (userHasProfile)
        {
            // پروفایل از قبل کامل است - فقط توکن جدید صادر کن
            // این برای حالتی است که event قبلاً process شده ولی کاربر توکن قدیمی دارد
            var refreshResult = await _tokenService.IssueAsync(request.UserId, ct);
            if (!refreshResult.IsSuccess)
            {
                return Result<RefreshTokenResponse>.Failure(
                    refreshResult.GetFirstError() ?? "خطا در صدور توکن",
                    refreshResult.ErrorCode ?? ErrorCodes.Common.OPERATION_FAILED);
            }
            return Result<RefreshTokenResponse>.Success(refreshResult.Value!);
        }

        // 2. چک کنیم آیا Customer برای این UserId وجود دارد یا نه
        var existingCustomer = await _customerQuery.GetByUserIdAsync(request.UserId, ct);
        
        Guid customerId;
        
        if (existingCustomer is not null)
        {
            // Customer وجود دارد ولی User.CustomerId نیست (داده ناهماهنگ)
            // فقط لینک می‌کنیم
            customerId = existingCustomer.Id.Value;
        }
        else
        {
            // 3. ساخت Customer جدید
            var customer = Customer.Register(
                userId: request.UserId,
                fullName: request.FullName,
                email: request.Email,
                phone: null, 
                correlationId: null
            );

            // 4. ذخیره Customer در دیتابیس
            await _customerCommand.AddAsync(customer, ct);
            await _unitOfWork.SaveChangesWithOutboxAsync(ct);
            
            customerId = customer.Id.Value;
        }

        // 5. لینک کردن CustomerId به User
        var linkResult = await _userProfile.LinkCustomerToUserAsync(request.UserId, customerId, ct);
        if (!linkResult.IsSuccess)
        {
            // اگر قبلاً لینک شده بود، فقط توکن جدید صادر کن
            if (linkResult.ErrorCode == ErrorCodes.Profile.PROFILE_ALREADY_COMPLETE)
            {
                var refreshResult = await _tokenService.IssueAsync(request.UserId, ct);
                if (refreshResult.IsSuccess)
                    return Result<RefreshTokenResponse>.Success(refreshResult.Value!);
            }
            
            return Result<RefreshTokenResponse>.Failure(
                linkResult.GetFirstError() ?? "خطا در لینک پروفایل",
                linkResult.ErrorCode ?? ErrorCodes.Profile.CUSTOMER_CREATION_FAILED);
        }

        // 6. صدور توکن‌های جدید (با profile_setup: done)
        var tokensResult = await _tokenService.IssueAsync(request.UserId, ct);
        if (!tokensResult.IsSuccess)
        {
            return Result<RefreshTokenResponse>.Failure(
                tokensResult.GetFirstError() ?? "خطا در صدور توکن",
                tokensResult.ErrorCode ?? ErrorCodes.Common.OPERATION_FAILED);
        }

        return Result<RefreshTokenResponse>.Success(tokensResult.Value!);
    }
}

