namespace DigiTekShop.Contracts.Abstractions.Identity.Auth
{
    public interface IPasswordHistoryService
    {
        Task<bool> AddAsync(Guid userId, string passwordHash, int? keepLastN = null, CancellationToken ct = default);

        Task<IReadOnlyList<PasswordHistoryEntryDto>> GetAsync(Guid userId, int count = 10,
            CancellationToken ct = default);

        Task<int> TrimAsync(Guid userId, int keepLastN, CancellationToken ct = default);
        Task<bool> ClearAsync(Guid userId, CancellationToken ct = default);

        Task<bool> ExistsInHistoryAsync(Guid userId, string plainPassword, int? maxToCheck = null,
            CancellationToken ct = default);

        Task<int> GetHistoryCountAsync(Guid userId, CancellationToken ct = default);

        Task<int> CleanupOldHistoryAsync(Guid userId, TimeSpan olderThan, CancellationToken ct = default);
    }
}
