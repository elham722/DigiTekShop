namespace DigiTekShop.Contracts.Abstractions.Identity.EmailConfirmation
{
    public interface IEmailConfirmationService
    {
       
        Task<Result> SendAsync(string userId, CancellationToken ct = default);
        Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default);
        Task<Result> ResendAsync(ResendEmailConfirmationRequestDto request, CancellationToken ct = default);
    }
}
