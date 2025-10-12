namespace DigiTekShop.Contracts.Abstractions.Repositories.Common.UnitOfWork;

public interface IAuditContext
{
    string? UserId { get; }
    string? UserName { get; }
    DateTime UtcNow { get; }
}