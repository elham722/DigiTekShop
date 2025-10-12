namespace DigiTekShop.Contracts.Abstractions.Identity.Registration
{
    public interface IRegistrationService
    {
        Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);
    }
}
