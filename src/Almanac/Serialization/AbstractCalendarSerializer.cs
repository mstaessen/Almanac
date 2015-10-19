using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Almanac.Model;
using Almanac.Model.Abstractions;
using Almanac.Serialization.Abstractions;
using Almanac.Serialization.Protocol;

namespace Almanac.Serialization
{
    public abstract class AbstractCalendarSerializer<TCalendar, TTimeZone, TEvent>
        where TTimeZone : ITimeZone
        where TEvent : AbstractEvent<TTimeZone>
        where TCalendar : AbstractCalendar<TTimeZone, TEvent>
    {
        private readonly ComponentSerializer serializer;

        protected AbstractCalendarSerializer()
            : this(new ComponentSerializer()) {}

        protected AbstractCalendarSerializer(ComponentSerializer serializer)
        {
            this.serializer = serializer;
        }

        public void Serialize(IEnumerable<TCalendar> calendars, TextWriter writer)
        {
            foreach (var calendar in calendars) {
                Serialize(calendar, writer);
            }
        }

        public void Serialize(TCalendar calendar, TextWriter writer)
        {
            serializer.Serialize(SerializeCalendar(calendar), writer);
        }

        public IEnumerable<TCalendar> Deserialize(string input)
        {
            return serializer.Deserialize(input).Select(DeserializeCalendarComponent);
        }

        public IEnumerable<TCalendar> Deserialize(TextReader reader)
        {
            return serializer.Deserialize(reader).Select(DeserializeCalendarComponent);
        }

        protected abstract Component SerializeCalendar(TCalendar calendar);

        protected abstract Component SerializeTimeZone(BclTimeZone bclTimeZone, DateTime minDate, DateTime maxDate);

        protected abstract Component SerializeEvent(TEvent @event);

        protected abstract TCalendar DeserializeCalendarComponent(Component component);

        protected virtual Property CreateDateTimeProperty(string propertyName, DateTime datetime)
        {
            return new Property(propertyName) {
                Value = datetime.ToICalDateTime() // TODO Clean up
            };
        }

        protected virtual Property CreateDateTimeProperty(string propertyName, ZonedDateTime<TTimeZone> datetime)
        {
            var property = new Property(propertyName) {
                Value = datetime.DateTime.ToICalDateTime() // TODO Clean up
            };
            if (!datetime.IsUtc) {
                var parameter = new Parameter(ParameterName.TimeZoneIdentifier) { Quoted = true };
                parameter.AddValue(datetime.TimeZone.Id);
                property.AddParameter(parameter);
            }
            return property;
        }
    }
}