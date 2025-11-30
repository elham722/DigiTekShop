namespace DigiTekShop.Contracts.Abstractions.Profile;

public interface ICustomerProfileRepository
{
    Task<bool> ExistsAsync(Guid customerId, CancellationToken ct = default);

    Task<CustomerProfileData?> GetByIdAsync(Guid customerId, CancellationToken ct = default);

    Task<CustomerProfileData?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<Guid> CreateAsync(CreateCustomerData data, CancellationToken ct = default);

    Task<bool> UpdateAsync(Guid customerId, UpdateCustomerData data, CancellationToken ct = default);
}

/// <summary>
/// داده‌های Customer برای پروفایل
/// </summary>
public sealed record CustomerProfileData
{
    public required Guid CustomerId { get; init; }
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

/// <summary>
/// داده‌های ساخت Customer
/// </summary>
public sealed record CreateCustomerData
{
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

/// <summary>
/// داده‌های بروزرسانی Customer
/// </summary>
public sealed record UpdateCustomerData
{
    public required string FullName { get; init; }
    public string? Email { get; init; }
}

