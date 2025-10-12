namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface ILoginAttemptService
    {
        Task<Result<LoginAttemptDto>> RecordLoginAttemptAsync(
            Guid? userId,
            LoginStatus status,
            string? ipAddress = null,
            string? userAgent = null,
            string? loginNameOrEmail = null,
            CancellationToken ct = default);

        Task<Result<IEnumerable<LoginAttemptDto>>> GetUserLoginAttemptsAsync(
            Guid userId,
            int limit = 50,
            CancellationToken ct = default);

        Task<Result<IEnumerable<LoginAttemptDto>>> GetLoginAttemptsByLoginNameAsync(
            string loginNameOrEmail,
            int limit = 50,
            CancellationToken ct = default);

        Task<Result<int>> GetFailedAttemptsFromIpAsync(
            string ipAddress,
            TimeSpan timeWindow,
            CancellationToken ct = default);

        Task<Result<int>> CleanupOldAttemptsAsync(
            TimeSpan olderThan,
            CancellationToken ct = default);
    }

}
