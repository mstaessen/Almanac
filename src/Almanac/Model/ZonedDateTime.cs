using System;
using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class ZonedDateTime<TTimeZone> where TTimeZone : ITimeZone
    {
        public DateTime DateTime { get; }

        public TTimeZone TimeZone { get; }

        public bool IsUtc { get; }

        internal ZonedDateTime(TTimeZone timezone, DateTime datetime)
        {
            if (timezone == null) {
                throw new ArgumentNullException(nameof(timezone));
            }
            TimeZone = timezone;
            DateTime = datetime;
        }

        public virtual DateTimeOffset ToDateTimeOffset()
        {
            return new DateTimeOffset(DateTime, TimeZone.GetUtcOffset(DateTime));
        }
    }
}