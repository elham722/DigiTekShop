namespace DigiTekShop.Contracts.Abstractions.Paging
{
    public sealed record FilterRule(string Field, string Op, string? Value);
}
