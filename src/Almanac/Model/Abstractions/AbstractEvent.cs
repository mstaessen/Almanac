using System;
using System.Collections.Generic;

namespace Almanac.Model.Abstractions
{
    public abstract class AbstractEvent<TTimeZone> where TTimeZone : ITimeZone
    {
        private readonly IList<Attendee> attendees = new List<Attendee>(); 

        public string Id { get; internal set; }

        public ZonedDateTime<TTimeZone> Start { get; set; }

        public ZonedDateTime<TTimeZone> End { get; set; }

        public TimeSpan Duration => End.ToDateTimeOffset() - Start.ToDateTimeOffset();

        public Classification Classification { get; set; }

        public DateTime CreatedAt { get; internal set; }

        public DateTime UpdatedAt { get; internal set; }

        public string Summary { get; set; }

        public string Description { get; set; }

        public string Location { get; set; }

        public IEnumerable<Attendee> Attendees => attendees;

        public bool IsAllDay { get; set; }

        public Attendee Organizer { get; set; }

        protected AbstractEvent()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }
    }
}