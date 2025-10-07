using DigiTekShop.Contracts.DTOs.Auth.Login;
using DigiTekShop.Contracts.DTOs.Auth.Register;
using DigiTekShop.Contracts.DTOs.Auth.Token;
using DigiTekShop.Contracts.Interfaces.Identity;
using DigiTekShop.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DigiTekShop.Identity.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtTokenService _jwt; // موجود در لایه Identity شما

    public AuthService(UserManager<User> userManager, SignInManager<User> signInManager, IJwtTokenService jwt)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
    }

    public Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto req, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TokenResponseDto>> RefreshAsync(RefreshRequestDto req, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<TokenResponseDto>> RegisterAsync(RegisterRequestDto req, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> RevokeAsync(RevokeRequestDto req, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
