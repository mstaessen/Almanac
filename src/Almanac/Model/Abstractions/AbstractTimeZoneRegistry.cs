using System.Collections.Generic;

namespace Almanac.Model.Abstractions
{
    public abstract class AbstractTimeZoneRegistry<TTimeZone> 
        where TTimeZone : ITimeZone
    {
        private readonly IDictionary<string, TTimeZone> registry = new Dictionary<string, TTimeZone>();

        public TTimeZone this[string timeZoneId] {
            get {
                if (!registry.ContainsKey(timeZoneId)) {
                    Register(CreateTimeZone(timeZoneId));
                }
                return registry[timeZoneId];
            }
        }

        public abstract TTimeZone Local { get; }

        public abstract TTimeZone Utc { get; }

        protected abstract TTimeZone CreateTimeZone(string timeZoneId);

        public void Register(TTimeZone timezone)
        {
            registry[timezone.Id] = timezone;
        }
    }
}