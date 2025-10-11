namespace DigiTekShop.Contracts.Repositories.Abstractions;

public interface IAuditContext
{
    string? UserId { get; }
    string? UserName { get; }
    DateTime UtcNow { get; }
}