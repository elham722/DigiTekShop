using DigiTekShop.Contracts.Abstractions.Time;

namespace DigiTekShop.Infrastructure.Time
{
    public sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Now => DateTime.Now;

        public DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);
        public DateOnly TodayLocal => DateOnly.FromDateTime(DateTime.Now);

        public DateTime ToLocalTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Utc)
                return utcDateTime.ToLocalTime();

            return utcDateTime.Kind == DateTimeKind.Local
                ? utcDateTime
                : TimeZoneInfo.ConvertTime(utcDateTime, TimeZoneInfo.Local);
        }


        public DateTime ToUtcTime(DateTime localDateTime)
        {
            return localDateTime.Kind switch
            {
                DateTimeKind.Utc => localDateTime,
                DateTimeKind.Local => localDateTime.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(localDateTime, DateTimeKind.Local).ToUniversalTime(),
                _ => localDateTime
            };
        }

        public DateTime ToLocalTime(DateTime utcDateTime, TimeZoneInfo tz)
        {
            var utc = utcDateTime.Kind switch
            {
                DateTimeKind.Utc => utcDateTime,
                DateTimeKind.Local => utcDateTime.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Unspecified),
                _ => utcDateTime
            };

            return utc.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(utc, tz)
                : TimeZoneInfo.ConvertTime(utc, tz);
        }

        public DateTime ToUtcTime(DateTime localDateTime, TimeZoneInfo tz)
        {
            return localDateTime.Kind switch
            {
                DateTimeKind.Utc => localDateTime,
                DateTimeKind.Local => localDateTime.ToUniversalTime(),
                DateTimeKind.Unspecified => TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), tz),
                _ => localDateTime
            };
        }
    }
}
