using System;
using System.Collections.Generic;
using System.Linq;

namespace Almanac.Model.Abstractions
{
    public abstract class AbstractEvent<TTimeZone> : IEquatable<AbstractEvent<TTimeZone>> 
        where TTimeZone : ITimeZone
    {
        private readonly IList<Attendee> attendees = new List<Attendee>(); 

        public string Id { get; internal set; }

        public ZonedDateTime<TTimeZone> Start { get; set; }

        public ZonedDateTime<TTimeZone> End { get; set; }

        public TimeSpan? Duration => End != null && Start != null 
            ? End.ToDateTimeOffset() - Start.ToDateTimeOffset()
            : (TimeSpan?) null;

        public Classification Classification { get; set; }

        public Priority Priority { get; set; }

        public DateTime CreatedAt { get; internal set; }

        public DateTime UpdatedAt { get; internal set; }

        public LocalizedString Summary { get; set; }

        public LocalizedString Description { get; set; }

        public LocalizedString Location { get; set; }

        public Position Geo { get; set; }

        public IEnumerable<Attendee> Attendees => attendees;

        public bool IsAllDay { get; set; }

        public Organizer Organizer { get; set; }

        protected AbstractEvent()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddAttendee(Attendee newAttendee)
        {
            attendees.Add(newAttendee);    
        }

        public void AddAttendee(IEnumerable<Attendee> newAttendees)
        {
            foreach (var attendee in newAttendees) {
                AddAttendee(attendee);
            }
        }

        public void RemoveAttendee(Attendee attendee)
        {
            attendees.Remove(attendee);
        }


        public bool Equals(AbstractEvent<TTimeZone> other)
        {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return String.Equals(Id, other.Id)
                && Equals(Summary, other.Summary)
                && Equals(Description, other.Description)
                && Equals(Classification, other.Classification)
                && Equals(Organizer, other.Organizer)
                && attendees.SequenceEqual(other.attendees)
                && Equals(Location, other.Location)
                && Equals(Geo, other.Geo)
                && Equals(Start, other.Start)
                && Equals(End, other.End)
                && IsAllDay == other.IsAllDay;
                // && CreatedAt.Equals(other.CreatedAt)
                // && UpdatedAt.Equals(other.UpdatedAt);
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
            return Equals((AbstractEvent<TTimeZone>) obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                var hashCode = attendees?.GetHashCode() ?? 0;
                return hashCode;
            }
        }
    }

    public class Organizer : Address
    {
        public Organizer(string email) 
            : this(email, null) {}

        public Organizer(string email, LocalizedString name)
            : base(email, name) {}
    }
}