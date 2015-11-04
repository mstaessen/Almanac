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
                Value = FormatDateTime(datetime)
            };
        }

        protected virtual Property CreateUtcDateTimeProperty(string propertyName, DateTime datetime)
        {
            if (datetime.Kind != DateTimeKind.Utc) {
                datetime = datetime.ToUniversalTime();
            }
            return new Property(propertyName) {
                Value = FormatUtcDateTime(datetime)
            };
        }

        protected virtual Property CreateDateTimeProperty(string propertyName, ZonedDateTime<TTimeZone> datetime)
        {
            var property = new Property(propertyName) {
                Value = FormatDateTime(datetime.DateTime)
            };
            if (!datetime.IsUtc) {
                var parameter = new Parameter(ParameterName.TimeZoneIdentifier) { Quoted = true };
                parameter.AddValue(datetime.TimeZone.Id);
                property.AddParameter(parameter);
            }
            return property;
        }

        protected virtual Property CreateLocalizedStringProperty(string propertyName, LocalizedString localString)
        {
            var property = new Property(propertyName) {
                Value = localString.Text
            };
            if (localString.CultureInfo != null) {
                var parameter = new Parameter(ParameterName.Language);
                parameter.AddValue(localString.CultureInfo.Name);
                property.AddParameter(parameter);
            }
            return property;
        }

        protected static string FormatDateTime(DateTime datetime)
        {
            return datetime.Kind == DateTimeKind.Utc
                ? FormatUtcDateTime(datetime)
                : datetime.ToString("yyyyMMdd'T'HHmmss");
        }

        protected static string FormatUtcDateTime(DateTime datetime)
        {
            return datetime.ToString("yyyyMMdd'T'HHmmss'Z'");
        }
    }
}