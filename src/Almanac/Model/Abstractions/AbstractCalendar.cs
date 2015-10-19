using System;
using System.Collections.Generic;
using System.Linq;

namespace Almanac.Model.Abstractions
{
    public abstract class AbstractCalendar<TTimeZone, TEvent> : IEquatable<AbstractCalendar<TTimeZone, TEvent>> 
        where TTimeZone : ITimeZone 
        where TEvent : AbstractEvent<TTimeZone>
    {
        private readonly IList<TEvent> events = new List<TEvent>();

        public IEnumerable<TEvent> Events => events;

        public Method Method { get; set; }

        public virtual string ProductIdentifier { get; internal set; }

        public void AddEvent(TEvent @event)
        {
            events.Add(@event);
        }

        public virtual bool Equals(AbstractCalendar<TTimeZone, TEvent> other)
        {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return Equals(Method, other.Method) 
                && events.SequenceEqual(other.events);
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
            return Equals((AbstractCalendar<TTimeZone, TEvent>) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                return (Method?.GetHashCode() ?? 0) ^ ((events?.GetHashCode() ?? 0) * 397);
            }
        }
    }
}