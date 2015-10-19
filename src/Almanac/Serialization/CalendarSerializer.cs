using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Almanac.Model;
using Almanac.Serialization.Abstractions;
using Almanac.Serialization.Protocol;
using Calendar = Almanac.Model.Calendar;

namespace Almanac.Serialization
{
    public class CalendarSerializer : AbstractCalendarSerializer<Calendar, BclTimeZone, Event>
    {
        private static readonly Lazy<string> ProductIdentifier = new Lazy<string>(CreateProductIdentifier);
        private static readonly Version SerializerVersion = new Version("2.0");

        private static string CreateProductIdentifier()
        {
            var version = GitVersionInformation.FullSemVer;
            return $"-//Almanac//{version}//EN";
        }

        protected override Component SerializeCalendar(Calendar calendar)
        {
            var component = new Component(ComponentName.Calendar);

            component.AddProperty(new Property(PropertyName.ProductIdentifier) {Value = ProductIdentifier.Value});
            component.AddProperty(new Property(PropertyName.Version) {Value = "2.0"});
            if (calendar.Method != null) {
                component.AddProperty(new Property(PropertyName.Method) {Value = calendar.Method.Value});
            }

            var dates = calendar.Events.SelectMany(e => new[] {e.Start, e.End}).Where(d => d != null);
            var timezones = dates.GroupBy(d => d.TimeZone).Select(g => new {
                TimeZone = g.Key,
                MinDate = g.Min(e => e.DateTime),
                MaxDate = g.Max(e => e.DateTime)
            });
            foreach (var timezone in timezones) {
                component.AddComponent(SerializeTimeZone(timezone.TimeZone, timezone.MinDate, timezone.MaxDate));
            }

            foreach (var @event in calendar.Events) {
                component.AddComponent(SerializeEvent(@event));
            }

            // TODO Other calendar components

            return component;
        }

        protected override Component SerializeTimeZone(BclTimeZone timezone, DateTime minDate, DateTime maxDate)
        {
            var component = new Component(ComponentName.TimeZone);
            SerializeTimeZoneId(timezone, component);
            SerializeTimeZoneLastUpdate(timezone, component);
            SerializeTimeZoneUrl(timezone, component);
            SerializeTimeZoneNonStandardProperties(timezone, component);
            SerializeTimeZoneIanaProperties(timezone, component);
            SerializeTimeZoneObservanceRules(timezone, component, minDate, maxDate);
            return component;
        }

        protected virtual void SerializeTimeZoneId(BclTimeZone timezone, Component component)
        {
            if (String.IsNullOrWhiteSpace(timezone.Id)) {
                throw new ArgumentException("The time zone identifier is required.");
            }
            component.AddProperty(new Property(PropertyName.TimeZoneId) {Value = timezone.Id});
        }

        protected virtual void SerializeTimeZoneLastUpdate(BclTimeZone timezone, Component component) {}

        protected virtual void SerializeTimeZoneUrl(BclTimeZone timezone, Component component) {}

        protected virtual void SerializeTimeZoneObservanceRules(BclTimeZone timezone, Component component, DateTime minDate, DateTime maxDate)
        {
            if (timezone.Info.SupportsDaylightSavingTime) {
                foreach (var observanceComponent in SerializeAdjustmentRules(timezone.Info, minDate, maxDate)) {
                    component.AddComponent(observanceComponent);
                }
            } else {
                component.AddComponent(new Component(ComponentName.TimeZoneStandard) {
                    // TODO 
                });
            }
        }

        protected virtual void SerializeTimeZoneNonStandardProperties(BclTimeZone timezone, Component component) {}

        protected virtual void SerializeTimeZoneIanaProperties(BclTimeZone timezone, Component component) {}

        private static IEnumerable<Component> SerializeAdjustmentRules(TimeZoneInfo timezone, DateTime earliest, DateTime latest)
        {
            var rules = timezone.GetAdjustmentRules().Where(ar => ar.DateEnd > earliest && ar.DateStart < latest).OrderBy(ar => ar.DateStart).ToArray();
            foreach (var rule in rules) {
                // Standard Time
                var standardTimeComponent = new Component(ComponentName.TimeZoneStandard);
                var standardTimeStart = CalculateFirstOnset(rule.DaylightTransitionEnd, rule.DateStart);
                standardTimeComponent.AddProperty(new Property(PropertyName.DateStart) {Value = standardTimeStart.ToICalDateTime()});
                var standardTimeRecurrenceRule = CreateRecurrenceRule(rule.DaylightTransitionEnd);
                if (rule.DateEnd < DateTime.MaxValue.Date) {
                    var end = CalculateLastOnset(rule.DaylightTransitionEnd, rule.DateEnd);
                    if (end > standardTimeStart) {
                        standardTimeRecurrenceRule += ";UNTIL=" + end.ToUniversalTime();
                    } else {
                        // TODO: single occurence
                    }
                }
                standardTimeComponent.AddProperty(new Property(PropertyName.RecurrenceRule) {Value = standardTimeRecurrenceRule});
                var standardTimeOffsetFrom = timezone.BaseUtcOffset + rule.DaylightDelta;
                standardTimeComponent.AddProperty(new Property(PropertyName.TimeZoneOffsetFrom) {Value = FormatOffset(standardTimeOffsetFrom)});
                var standardTimeOffsetTo = timezone.BaseUtcOffset;
                standardTimeComponent.AddProperty(new Property(PropertyName.TimeZoneOffsetTo) {Value = FormatOffset(standardTimeOffsetTo)});
                yield return standardTimeComponent;

                var daylightTimeComponent = new Component(ComponentName.TimeZoneDaylight);
                var daylightTimeStart = CalculateFirstOnset(rule.DaylightTransitionStart, rule.DateStart);
                daylightTimeComponent.AddProperty(new Property(PropertyName.DateStart) {Value = daylightTimeStart.ToICalDateTime()});
                var daylightTimeRecurrenceRule = CreateRecurrenceRule(rule.DaylightTransitionStart);
                if (rule.DateEnd < DateTime.MaxValue.Date) {
                    var end = CalculateLastOnset(rule.DaylightTransitionStart, rule.DateEnd);
                    if (end > daylightTimeStart) {
                        daylightTimeRecurrenceRule += ";UNTIL=" + end.ToUniversalTime();
                    } else {
                        // TODO: single occurence
                    }
                }
                daylightTimeComponent.AddProperty(new Property(PropertyName.RecurrenceRule) {Value = daylightTimeRecurrenceRule});
                var daylightTimeOffsetFrom = timezone.BaseUtcOffset;
                daylightTimeComponent.AddProperty(new Property(PropertyName.TimeZoneOffsetFrom) {Value = FormatOffset(daylightTimeOffsetFrom)});
                var daylightTimeOffsetTo = timezone.BaseUtcOffset + rule.DaylightDelta;
                daylightTimeComponent.AddProperty(new Property(PropertyName.TimeZoneOffsetTo) {Value = FormatOffset(daylightTimeOffsetTo)});
                yield return daylightTimeComponent;
            }
        }

        private static string CreateRecurrenceRule(TimeZoneInfo.TransitionTime transitionTime)
        {
            if (transitionTime.IsFixedDateRule) {
                return $"FREQ=YEARLY;BYDAY={transitionTime.Day};BYMONTH={transitionTime.Month}";
            }
            var byDay = transitionTime.Week == 5 ? "-1" : $"{transitionTime.Week}";
            var days = new[] {"SU", "MO", "TU", "WE", "TH", "SA"};
            return $"FREQ=YEARLY;BYDAY={byDay}{days[(int) transitionTime.DayOfWeek]};BYMONTH={transitionTime.Month}";
        }

        private static DateTime CalculateFirstOnset(TimeZoneInfo.TransitionTime transition, DateTime start)
        {
            var onset = CalculateTransitionDate(transition, start.Year);
            if (onset < start) {
                onset = CalculateTransitionDate(transition, start.Year + 1);
            }
            return onset;
        }

        private static DateTime CalculateLastOnset(TimeZoneInfo.TransitionTime transition, DateTime end)
        {
            var onset = CalculateTransitionDate(transition, end.Year);
            if (onset > end) {
                onset = CalculateTransitionDate(transition, end.Year - 1);
            }
            return onset;
        }


        private static DateTime CalculateTransitionDate(TimeZoneInfo.TransitionTime transition, int year)
        {
            if (transition.IsFixedDateRule) {
                return new DateTime(year, transition.Month, transition.Day).Add(transition.TimeOfDay.TimeOfDay);
            }
            return CalculateFloatingTransitionDate(transition, year);
        }

        private static DateTime CalculateFloatingTransitionDate(TimeZoneInfo.TransitionTime transition, int year)
        {
            var calendar = CultureInfo.InvariantCulture.Calendar;
            var dayOfWeekOnFirstOfMonth = (int) calendar.GetDayOfWeek(new DateTime(year, transition.Month, 1));
            var dayOfWeekOnTransition = (int) transition.DayOfWeek;
            var effectiveDay = (dayOfWeekOnFirstOfMonth > dayOfWeekOnTransition) ? 1 + (dayOfWeekOnTransition + 7 - dayOfWeekOnFirstOfMonth) : 1 + (dayOfWeekOnTransition - dayOfWeekOnFirstOfMonth);
            effectiveDay += (transition.Week - 1) * 7;
            if (effectiveDay > calendar.GetDaysInMonth(year, transition.Month)) {
                effectiveDay -= 7;
            }
            return new DateTime(year, transition.Month, effectiveDay).Add(transition.TimeOfDay.TimeOfDay);
        }

        private static string FormatOffset(TimeSpan timespan)
        {
            var sign = timespan < TimeSpan.Zero ? "-" : "+";
            return $"{sign}{timespan.ToString("hhmm")}";
        }

        protected override Component SerializeEvent(Event @event)
        {
            var component = new Component(ComponentName.Event);
            SerializeEventDateTimeStamp(@event, component);
            SerializeEventId(@event, component);
            SerializeEventStart(@event, component);
            SerializeEventClassification(@event, component);
            SerializeEventCreationDate(@event, component);
            SerializeEventDescription(@event, component);
            SerializeEventGeo(@event, component);
            SerializeEventLastUpdate(@event, component);
            SerializeEventLocation(@event, component);
            SerializeEventOrganizer(@event, component);
            SerializeEventPriority(@event, component);
            SerializeEventSequence(@event, component);
            SerializeEventStatus(@event, component);
            SerializeEventSummary(@event, component);
            SerializeEventTransparency(@event, component);
            SerializeEventUrl(@event, component);
            SerializeEventRecurrenceId(@event, component);
            SerializeEventRecurrenceRule(@event, component);
            SerializeEventEnd(@event, component);
            SerializeEventDuration(@event, component);
            SerializeEventAttachments(@event, component);
            SerializeEventAttendees(@event, component);
            SerializeEventCategories(@event, component);
            SerializeEventComments(@event, component);
            SerializeEventContacts(@event, component);
            SerializeEventExceptionDates(@event, component);
            SerializeEventRequestStatus(@event, component);
            SerializeEventRelatedTo(@event, component);
            SerializeEventResources(@event, component);
            SerializeEventRecurrenceDateTimes(@event, component);
            SerializeEventNonStandardProperties(@event, component);
            SerializeEventIanaProperties(@event, component);
            return component;
        }

        protected virtual void SerializeEventDateTimeStamp(Event @event, Component component)
        {
            var property = CreateDateTimeProperty(PropertyName.DateTimeStamp, DateTime.UtcNow);
            component.AddProperty(property);
        }

        protected virtual void SerializeEventId(Event @event, Component component)
        {
            if (String.IsNullOrWhiteSpace(@event.Id)) {
                throw new ArgumentException("The event identifier is a required property.");
            }
            var property = new Property(PropertyName.UniqueIdentifier) {Value = @event.Id};
            component.AddProperty(property);
        }

        protected virtual void SerializeEventStart(Event @event, Component component)
        {
            if (@event.Start == null) {
                throw new ArgumentException("The event start date-time is required.");
            }
            var property = CreateDateTimeProperty(PropertyName.DateEnd, @event.Start);
            component.AddProperty(property);
        }

        protected virtual void SerializeEventClassification(Event @event, Component component) {}

        protected virtual void SerializeEventCreationDate(Event @event, Component component) {}

        protected virtual void SerializeEventDescription(Event @event, Component component)
        {
            if (!String.IsNullOrWhiteSpace(@event.Description)) {
                var property = new Property(PropertyName.Description) {Value = @event.Description};
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventGeo(Event @event, Component component) {}

        protected virtual void SerializeEventLastUpdate(Event @event, Component component) {}

        protected virtual void SerializeEventLocation(Event @event, Component component)
        {
            if (!String.IsNullOrWhiteSpace(@event.Location)) {
                var property = new Property(PropertyName.Location) {Value = @event.Location};
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventOrganizer(Event @event, Component component)
        {
            if (@event.Organizer != null) {
                var property = new Property(PropertyName.Organizer) {
                    Value = $"mailto:{@event.Organizer.Email}"
                };
                var parameter = new Parameter(ParameterName.CommonName) {
                    Quoted = true,
                };
                parameter.AddValue(@event.Organizer.Name);
                property.AddParameter(parameter);
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventPriority(Event @event, Component component) {}

        protected virtual void SerializeEventSequence(Event @event, Component component) {}

        protected virtual void SerializeEventStatus(Event @event, Component component) {}

        protected virtual void SerializeEventSummary(Event @event, Component component)
        {
            if (!String.IsNullOrWhiteSpace(@event.Summary)) {
                var property = new Property(PropertyName.Summary) {Value = @event.Summary};
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventTransparency(Event @event, Component component) {}

        protected virtual void SerializeEventUrl(Event @event, Component component) {}

        protected virtual void SerializeEventRecurrenceId(Event @event, Component component) {}

        protected virtual void SerializeEventRecurrenceRule(Event @event, Component component) {}

        protected virtual void SerializeEventEnd(Event @event, Component component)
        {
            if (@event.End == null) {
                throw new ArgumentException("The event end date-time is required.");
            }
            var property = CreateDateTimeProperty(PropertyName.DateEnd, @event.End);
            component.AddProperty(property);
        }

        protected virtual void SerializeEventDuration(Event @event, Component component) {}

        protected virtual void SerializeEventAttachments(Event @event, Component component) {}

        protected virtual void SerializeEventAttendees(Event @event, Component component)
        {
            foreach (var attendee in @event.Attendees) {
                var property = new Property(PropertyName.Attendee);
                // TODO
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventCategories(Event @event, Component component) {}

        protected virtual void SerializeEventComments(Event @event, Component component) {}

        protected virtual void SerializeEventContacts(Event @event, Component component) {}

        protected virtual void SerializeEventExceptionDates(Event @event, Component component) {}

        protected virtual void SerializeEventRequestStatus(Event @event, Component component) {}

        protected virtual void SerializeEventRelatedTo(Event @event, Component component) {}

        protected virtual void SerializeEventResources(Event @event, Component component) {}

        protected virtual void SerializeEventRecurrenceDateTimes(Event @event, Component component) {}

        protected virtual void SerializeEventNonStandardProperties(Event @event, Component component) {}

        protected virtual void SerializeEventIanaProperties(Event @event, Component component) {}

        protected override Calendar DeserializeCalendarComponent(Component component)
        {
            if (component == null) {
                throw new ArgumentNullException(nameof(component));
            }
            if (!String.Equals(component.Name, "VCALENDAR", StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("Supplied component is not a Calendar component.", nameof(component));
            }
            var calendar = CreateCalendar();
            DeserializeCalendarVersion(component, calendar);
            DeserializeCalendarProductIdentifier(component, calendar);
            DeserializeCalendarCalendarScale(component, calendar);
            DeserializeCalendarMethod(component, calendar);
            DeserializeCalendarNonStandardProperties(component, calendar);
            DeserializeCalendarIanaProperties(component, calendar);
            DeserializeCalendarTimeZones(component, calendar);
            DeserializeCalendarEvents(component, calendar);
            return calendar;
        }

        protected virtual Calendar CreateCalendar()
        {
            return new Calendar();
        }

        protected virtual void DeserializeCalendarVersion(Component component, Calendar calendar)
        {
            var property = component.Properties.SingleOrDefault(x => String.Equals(x.Name, PropertyName.Version, StringComparison.OrdinalIgnoreCase));
            if (property == null) {
                throw new ArgumentException("The version property is required.");
            }
            var match = Regex.Match(property.Value, "^(([0-9]\\.[0-9]);)?([0-9]\\.[0-9])$");
            if (!match.Success) {
                throw new ArgumentException("Invalid version specified.");
            }
            var maxVersion = new Version(match.Groups[3].Value);
            var minVersion = match.Groups[2].Success
                ? new Version(match.Groups[2].Value)
                : maxVersion;
            if (maxVersion < SerializerVersion || minVersion > SerializerVersion) {
                throw new NotSupportedException("This serializer only supports version 2.0.");
            }
        }

        protected virtual void DeserializeCalendarProductIdentifier(Component component, Calendar calendar)
        {
            var property = component.Properties.SingleOrDefault(x => String.Equals(x.Name, PropertyName.ProductIdentifier, StringComparison.OrdinalIgnoreCase));
            if (property == null) {
                throw new ArgumentException("The product identifier is required.");
            }
            calendar.ProductIdentifier = property.Value;
        }

        protected virtual void DeserializeCalendarCalendarScale(Component component, Calendar calendar)
        {
            var property = component.Properties.SingleOrDefault(x => String.Equals(x.Name, PropertyName.CalendarScale, StringComparison.OrdinalIgnoreCase));
            if (property != null) {
                if (!String.Equals(property.Value, "GREGORIAN", StringComparison.OrdinalIgnoreCase)) {
                    throw new ArgumentException("This serializer only supports the Gregorian calendar scale.");
                }
            }
        }

        protected virtual void DeserializeCalendarMethod(Component component, Calendar calendar)
        {
            var property = component.Properties.SingleOrDefault(x => String.Equals(x.Name, PropertyName.Method, StringComparison.OrdinalIgnoreCase));
            if (property != null) {
                // TODO use registry?
                calendar.Method = new Method {Value = property.Value};
            }
        }

        protected virtual void DeserializeCalendarNonStandardProperties(Component component, Calendar calendar) {}

        protected virtual void DeserializeCalendarIanaProperties(Component component, Calendar calendar) {}

        protected virtual void DeserializeCalendarTimeZones(Component component, Calendar calendar)
        {
            foreach (var timezoneComponent in component.Components.Where(x => String.Equals(x.Name, "VTIMEZONE", StringComparison.OrdinalIgnoreCase))) {
                DeserializeTimeZoneComponent(timezoneComponent, calendar);
            }
        }

        protected virtual void DeserializeCalendarEvents(Component component, Calendar calendar)
        {
            foreach (var eventComponent in component.Components.Where(x => String.Equals(x.Name, "VEVENT", StringComparison.OrdinalIgnoreCase))) {
                DeserializeEventComponent(eventComponent, calendar);
            }
        }

        protected virtual void DeserializeTimeZoneComponent(Component component, Calendar calendar)
        {
            // TODO
        }

        protected virtual void DeserializeEventComponent(Component component, Calendar calendar)
        {
            var @event = CreateEvent();

            calendar.AddEvent(@event);
        }

        protected virtual Event CreateEvent()
        {
            return new Event();
        }
    }
}