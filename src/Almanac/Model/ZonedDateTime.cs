using System;
using System.Collections.Generic;
using System.Diagnostics;
using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    [DebuggerDisplay("{DateTime} ({TimeZone})")]
    public class ZonedDateTime<TTimeZone> : IEquatable<ZonedDateTime<TTimeZone>> 
        where TTimeZone : ITimeZone
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

        public bool Equals(ZonedDateTime<TTimeZone> other)
        {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return DateTime.Equals(other.DateTime) 
                && Equals(TimeZone.Id, other.TimeZone.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((ZonedDateTime<TTimeZone>) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return (DateTime.GetHashCode() * 397) ^ EqualityComparer<TTimeZone>.Default.GetHashCode(TimeZone);
            }
        }
    }
}