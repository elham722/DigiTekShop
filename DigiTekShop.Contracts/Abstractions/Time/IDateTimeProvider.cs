namespace DigiTekShop.Contracts.Abstractions.Time
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }          
        DateTime Now { get; }           

        DateOnly TodayUtc { get; }       
        DateOnly TodayLocal { get; }       

        
        DateTime ToLocalTime(DateTime utcDateTime);
        DateTime ToLocalTime(DateTime utcDateTime, TimeZoneInfo tz);

        DateTime ToUtcTime(DateTime localDateTime);
        DateTime ToUtcTime(DateTime localDateTime, TimeZoneInfo tz);
    }
}
