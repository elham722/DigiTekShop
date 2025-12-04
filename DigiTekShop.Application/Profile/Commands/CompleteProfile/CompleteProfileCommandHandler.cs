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
        
        var userHasProfile = await _userProfile.HasProfileAsync(request.UserId, ct);
        if (userHasProfile)
        {
            
            return Result<RefreshTokenResponse>.Failure(
                "پروفایل قبلاً تکمیل شده است",
                ErrorCodes.Profile.PROFILE_ALREADY_COMPLETE);
        }

        
        var existingCustomer = await _customerQuery.GetByUserIdAsync(request.UserId, ct);
        
        Guid customerId;
        
        if (existingCustomer is not null)
        {
       
            customerId = existingCustomer.Id.Value;
        }
        else
        {
            
            var customer = Customer.Register(
                userId: request.UserId,
                fullName: request.FullName,
                email: request.Email,
                phone: null, 
                correlationId: null
            );

            
            await _customerCommand.AddAsync(customer, ct);
            await _unitOfWork.SaveChangesWithOutboxAsync(ct);
            
            customerId = customer.Id.Value;
        }

     
        var linkResult = await _userProfile.LinkCustomerToUserAsync(request.UserId, customerId, ct);
        if (!linkResult.IsSuccess)
        {
            return Result<RefreshTokenResponse>.Failure(
                linkResult.GetFirstError() ?? "خطا در لینک پروفایل",
                linkResult.ErrorCode ?? ErrorCodes.Profile.CUSTOMER_CREATION_FAILED);
        }

        
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

