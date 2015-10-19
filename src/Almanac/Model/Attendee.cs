namespace Almanac.Model
{
    public class Attendee
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public AttendeeType? Type { get; set; }

        public AttendeeRole? Role { get; set; }

        public EventParticipationStatus? Status { get; set; }

        public bool? ResponseExpectation { get; set; }
    }
}