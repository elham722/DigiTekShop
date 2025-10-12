namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface IRegistrationService
    {
        Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);
    }
}
