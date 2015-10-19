using System;
using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class BclTimeZoneRegistry : AbstractTimeZoneRegistry<BclTimeZone>
    {
        private static readonly BclTimeZone LocalTimeZone = new BclTimeZone(TimeZoneInfo.Local);

        private static readonly BclTimeZone UtcTimeZone = new BclTimeZone(TimeZoneInfo.Utc);

        public override BclTimeZone Local => LocalTimeZone;

        public override BclTimeZone Utc => UtcTimeZone;

        public BclTimeZoneRegistry()
        {
            Register(LocalTimeZone);
            Register(UtcTimeZone);
        }

        protected override BclTimeZone CreateTimeZone(string timeZoneId)
        {
            return new BclTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }
    }
}