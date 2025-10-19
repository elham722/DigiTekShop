namespace DigiTekShop.Contracts.Abstractions.Telemetry;

public interface ICorrelationContext
{
    string? GetCorrelationId();
    string? GetCausationId();
}