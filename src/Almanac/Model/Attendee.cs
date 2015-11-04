using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class Attendee : Address
    {
        public AttendeeType Type { get; set; }

        public AttendeeRole Role { get; set; }

        public ParticipationStatus ParticipationStatus { get; set; }

        public bool ResponseExpected { get; set; }

        public Attendee(string email) 
            : this(email, null) { }

        public Attendee(string email, LocalizedString name)
            : base(email, name)
        {
            Type = AttendeeType.Individual;
            Role = AttendeeRole.RequiredParticipant;
            ParticipationStatus = ParticipationStatus.NeedsAction;
        }
    }
}