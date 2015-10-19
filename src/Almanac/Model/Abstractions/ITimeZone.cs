using System;

namespace Almanac.Model.Abstractions
{
    public interface ITimeZone
    {
        string Id { get; }

        TimeSpan GetUtcOffset(DateTime datetime);
    }

    public interface IZonedDateTimeProvider<TTimeZone> 
        where TTimeZone : ITimeZone
    {
        ZonedDateTime<TTimeZone> CreateZonedDateTime(DateTime datetime);
    }
}