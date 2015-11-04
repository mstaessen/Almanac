using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Almanac.Model;
using Almanac.Model.Abstractions;
using Almanac.Serialization;
using Xunit;
using Calendar = Almanac.Model.Calendar;

namespace Almanac.Tests
{
    public class CalendarSerializationFacts
    {
        [Fact]
        public void SerializerCanRoundTripCalendars()
        {
            // Arrange
            var timeZoneRegistry = new BclTimeZoneRegistry();
            var calendar = new Calendar {
                Method = Method.Request 
            };
            var @event = new Event {
                Summary = new LocalizedString ("Text Event"),
                Description = new LocalizedString ("Event used for unit testing"),
                Start = timeZoneRegistry.Local.CreateZonedDateTime(new DateTime(2015, 10, 18, 9, 0, 0)),
                End = timeZoneRegistry.Local.CreateZonedDateTime(new DateTime(2015, 10, 18, 11, 0, 0)),
                Location = new LocalizedString ("Earth", new CultureInfo("en")),
                Classification = Classification.Public,
                Organizer = new Organizer("jane.doe@example.com", new LocalizedString("Doe, Jane"))
            };
            @event.AddAttendee(new Attendee("john.doe@example.com", new LocalizedString("Doe, John")));
            calendar.AddEvent(@event);

            // Act
            var serializer = new CalendarSerializer();
            using (var stream = new MemoryStream()) {
                using (var writer = new StreamWriter(stream)) {
                    serializer.Serialize(calendar, writer);
                    Debug.WriteLine(Encoding.UTF8.GetString(stream.GetBuffer()));
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream)) {
                        var result = serializer.Deserialize(reader).FirstOrDefault();
                        // Assert
                        Assert.Equal(calendar, result);
                    }
                }
            }
        }
    }
}