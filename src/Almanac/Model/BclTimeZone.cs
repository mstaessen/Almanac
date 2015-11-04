using System;
using System.Diagnostics;
using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    [DebuggerDisplay("{Id}")]
    public class BclTimeZone : ITimeZone, IZonedDateTimeProvider<BclTimeZone>
    {
        public static BclTimeZone Utc { get; } = new BclTimeZone(TimeZoneInfo.Utc);

        public static BclTimeZone Local { get; } = new BclTimeZone(TimeZoneInfo.Local);

        public TimeZoneInfo Info { get; }

        public string Id => Info.Id;

        public BclTimeZone(TimeZoneInfo timeInfo)
        {
            Info = timeInfo;
        }

        public TimeSpan GetUtcOffset(DateTime datetime)
        {
            return Info.GetUtcOffset(datetime);
        }

        public ZonedDateTime<BclTimeZone> CreateZonedDateTime(DateTime datetime)
        {
            return new ZonedDateTime<BclTimeZone>(this, datetime);
        }
    }
}