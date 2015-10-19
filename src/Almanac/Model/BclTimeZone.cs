using System;
using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class BclTimeZone : ITimeZone, IZonedDateTimeProvider<BclTimeZone>
    {
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