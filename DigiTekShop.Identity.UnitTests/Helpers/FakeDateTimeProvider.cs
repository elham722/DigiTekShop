namespace DigiTekShop.Identity.UnitTests.Helpers;

/// <summary>
/// Fake DateTime provider for testing time-dependent logic
/// </summary>
public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    private DateTime _currentTime;

    public FakeDateTimeProvider(DateTime? initialTime = null)
    {
        _currentTime = initialTime ?? new DateTime(2025, 11, 12, 10, 0, 0, DateTimeKind.Utc);
    }

    public DateTime UtcNow => _currentTime;
    
    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(_currentTime, TimeZoneInfo.Local);

    public DateOnly TodayUtc => DateOnly.FromDateTime(_currentTime);
    
    public DateOnly TodayLocal => DateOnly.FromDateTime(Now);

    public DateTime ToLocalTime(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZoneInfo.Local);
    }

    public DateTime ToLocalTime(DateTime utcDateTime, TimeZoneInfo tz)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tz);
    }

    public DateTime ToUtcTime(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.Local);
    }

    public DateTime ToUtcTime(DateTime localDateTime, TimeZoneInfo tz)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, tz);
    }

    // Helper methods for testing
    public void SetUtcNow(DateTime time)
    {
        _currentTime = DateTime.SpecifyKind(time, DateTimeKind.Utc);
    }

    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
    
    // Helper for DateTimeOffset
    public DateTimeOffset UtcNowOffset => new DateTimeOffset(_currentTime, TimeSpan.Zero);
}

