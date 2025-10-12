namespace DigiTekShop.Contracts.Auth.SecurityEvent
{
    public record SecurityEventStatsDto(
        int TotalEvents,
        int UnresolvedEvents,
        int HighSeverityEvents,
        int MediumSeverityEvents,
        int LowSeverityEvents,
        Dictionary<string, int> EventsByType,
        Dictionary<string, int> EventsByIp
    );
}
