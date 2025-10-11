namespace DigiTekShop.Contracts.DTOs.Pagination
{
    public sealed record FilterRule(string Field, string Op, string? Value);
}
