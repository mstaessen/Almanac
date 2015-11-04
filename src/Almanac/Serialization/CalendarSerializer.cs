using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Almanac.Model;
using Almanac.Model.Abstractions;
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

        // TODO: Cleanup code
        private static IEnumerable<Component> SerializeAdjustmentRules(TimeZoneInfo timezone, DateTime earliest, DateTime latest)
        {
            var rules = timezone.GetAdjustmentRules().Where(ar => ar.DateEnd > earliest && ar.DateStart < latest).OrderBy(ar => ar.DateStart).ToArray();
            foreach (var rule in rules) {
                // Standard Time
                var standardTimeComponent = new Component(ComponentName.TimeZoneStandard);
                var standardTimeStart = CalculateFirstOnset(rule.DaylightTransitionEnd, rule.DateStart);
                standardTimeComponent.AddProperty(new Property(PropertyName.DateTimeStart) {Value = FormatDateTime(standardTimeStart)});
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
                daylightTimeComponent.AddProperty(new Property(PropertyName.DateTimeStart) {Value = FormatDateTime(daylightTimeStart)});
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
            var property = CreateUtcDateTimeProperty(PropertyName.DateTimeStamp, DateTime.UtcNow);
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
            var property = CreateDateTimeProperty(PropertyName.DateTimeStart, @event.Start);
            component.AddProperty(property);
        }

        protected virtual void SerializeEventClassification(Event @event, Component component)
        {
            if (@event.Classification != null) {
                var property = new Property(PropertyName.Classification) {Value = @event.Classification.Value};
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventCreationDate(Event @event, Component component)
        {
            var property = CreateDateTimeProperty(PropertyName.Created, @event.CreatedAt);
            component.AddProperty(property);
        }

        protected virtual void SerializeEventDescription(Event @event, Component component)
        {
            if (@event.Description != null) {
                var property = SerializeLocalizedString(PropertyName.Description, @event.Description);
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventGeo(Event @event, Component component)
        {

            if (@event.Geo != null) {
                var property = new Property(PropertyName.Geo) {
                    Value = String.Format(CultureInfo.InvariantCulture, "{0};{1}", @event.Geo.Latitude, @event.Geo.Longitude)
                };
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventLastUpdate(Event @event, Component component)
        {
            var property = CreateUtcDateTimeProperty(PropertyName.LastModified, @event.UpdatedAt);
            component.AddProperty(property);
        }

        protected virtual void SerializeEventLocation(Event @event, Component component)
        {
            if (@event.Location != null) {
                var property = CreateLocalizedStringProperty(PropertyName.Location, @event.Location);
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
                parameter.AddValue(@event.Organizer.Name.Text);
                property.AddParameter(parameter);
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventPriority(Event @event, Component component)
        {
            if (@event.Priority != null) {
                var property = new Property(PropertyName.Priority) {
                    Value = @event.Priority.Value
                };
                component.AddProperty(property);
            }
        }

        protected virtual void SerializeEventSequence(Event @event, Component component) {}

        protected virtual void SerializeEventStatus(Event @event, Component component) {}

        protected virtual void SerializeEventSummary(Event @event, Component component)
        {
            if (@event.Summary != null) {
                var property = SerializeLocalizedString(PropertyName.Summary, @event.Summary);
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
            var property = CreateDateTimeProperty(PropertyName.DateTimeEnd, @event.End);
            component.AddProperty(property);
        }

        protected virtual void SerializeEventDuration(Event @event, Component component)
        {
            // This serializer does not use duration, but uses DTEND instead.
        }

        protected virtual void SerializeEventAttachments(Event @event, Component component) {}

        protected virtual void SerializeEventAttendees(Event @event, Component component)
        {
            foreach (var attendee in @event.Attendees) {
                var property = new Property(PropertyName.Attendee) {
                    Value = $"mailto:{attendee.Email}"
                };
                if (!String.IsNullOrEmpty(attendee.Name?.Text)) {
                    property.AddParameter(new Parameter(ParameterName.CommonName, attendee.Name.Text) { Quoted = true });
                    if (attendee.Name.CultureInfo != null) {
                        property.AddParameter(new Parameter(ParameterName.Language, attendee.Name.CultureInfo.Name));
                    }
                }
                if (attendee.Type != null) {
                    property.AddParameter(new Parameter(ParameterName.CalendarUserType, attendee.Type.Value));
                    if (Equals(attendee.Type, AttendeeType.Resource) || Equals(attendee.Type, AttendeeType.Room)) {
                        property.AddParameter(new Parameter(ParameterName.ParticipationRole, AttendeeRole.NonParticipant.Value));
                    }
                }
                if (attendee.Role != null && !property.Parameters[ParameterName.ParticipationRole].Any()) {
                    property.AddParameter(new Parameter(ParameterName.ParticipationRole, attendee.Role.Value));
                }
                if (attendee.ParticipationStatus != null) {
                    property.AddParameter(new Parameter(ParameterName.ParticipationStatus, attendee.ParticipationStatus.Value));
                }
                property.AddParameter(new Parameter(ParameterName.RsvpExpectation, attendee.ResponseExpected.ToString().ToUpperInvariant()));
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
            var context = new SerializationContext();
            var calendar = CreateCalendar();
            DeserializeCalendarVersion(component, calendar);
            DeserializeCalendarProductIdentifier(component, calendar);
            DeserializeCalendarCalendarScale(component, calendar);
            DeserializeCalendarMethod(component, calendar);
            DeserializeCalendarNonStandardProperties(component, calendar);
            DeserializeCalendarIanaProperties(component, calendar);
            DeserializeCalendarTimeZones(component, calendar, context);
            DeserializeCalendarEvents(component, calendar, context);
            return calendar;
        }

        protected virtual Calendar CreateCalendar()
        {
            return new Calendar();
        }

        protected virtual void DeserializeCalendarVersion(Component component, Calendar calendar)
        {
            var property = component.Properties[PropertyName.Version].SingleOrDefault();
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
            var property = component.Properties[PropertyName.ProductIdentifier].SingleOrDefault();
            if (property == null) {
                throw new ArgumentException("The product identifier is required.");
            }
            calendar.ProductIdentifier = property.Value;
        }

        protected virtual void DeserializeCalendarCalendarScale(Component component, Calendar calendar)
        {
            var property = component.Properties[PropertyName.CalendarScale].SingleOrDefault();
            if (property != null) {
                if (!String.Equals(property.Value, "GREGORIAN", StringComparison.OrdinalIgnoreCase)) {
                    throw new ArgumentException("This serializer only supports the Gregorian calendar scale.");
                }
            }
        }

        protected virtual void DeserializeCalendarMethod(Component component, Calendar calendar)
        {
            var property = component.Properties[PropertyName.Method].SingleOrDefault();
            if (property != null) {
                // TODO use registry?
                calendar.Method = new Method {Value = property.Value};
            }
        }

        protected virtual void DeserializeCalendarNonStandardProperties(Component component, Calendar calendar) {}

        protected virtual void DeserializeCalendarIanaProperties(Component component, Calendar calendar) {}

        protected virtual void DeserializeCalendarTimeZones(Component component, Calendar calendar, SerializationContext context)
        {
            foreach (var timezoneComponent in component.Components[ComponentName.TimeZone]) {
                DeserializeTimeZoneComponent(timezoneComponent, calendar, context);
            }
        }

        protected virtual void DeserializeCalendarEvents(Component component, Calendar calendar, SerializationContext context)
        {
            foreach (var eventComponent in component.Components[ComponentName.Event]) {
                DeserializeEventComponent(eventComponent, calendar, context);
            }
        }

        protected virtual void DeserializeTimeZoneComponent(Component component, Calendar calendar, SerializationContext context)
        {
            var idProperty = component.Properties[PropertyName.TimeZoneId].SingleOrDefault();
            if (idProperty == null) {
                throw new ArgumentException("The time zone identifier is a required property.");       
            }
            var id = idProperty.Value;

            try {
                if (!context.TimeZones.ContainsKey(id)) {
                    var tzi = TimeZoneInfo.FindSystemTimeZoneById(id);
                    context.TimeZones[id] = new BclTimeZone(tzi);
                }
            } catch (TimeZoneNotFoundException) {
                // TODO: Build custom time zone based on timezone components
            }
        }

        private TimeZoneObservance DeserializeTimeZoneObservanceComponent(Component component)
        {
            var observance = new TimeZoneObservance();
            var startProperty = component.Properties[PropertyName.DateTimeStart].SingleOrDefault();
            if (startProperty == null) {
                throw new ArgumentException("They date time start is a required time zone observance component property.");
            }
            observance.Start = DeserializeLocalDateTime(startProperty);

            var offsetFromProperty = component.Properties[PropertyName.TimeZoneOffsetFrom].SingleOrDefault();
            if (startProperty == null) {
                throw new ArgumentException("They offset from is a required time zone observance component property.");
            }
            observance.OffsetFrom = DeserializeTimeSpan(offsetFromProperty);

            var offsetToProperty = component.Properties[PropertyName.TimeZoneOffsetTo].SingleOrDefault();
            if (startProperty == null) {
                throw new ArgumentException("They offset from is a required time zone observance component property.");
            }
            observance.OffsetTo = DeserializeTimeSpan(offsetToProperty);

            var recurrenceProperty = component.Properties[PropertyName.RecurrenceRule].FirstOrDefault();
            if (recurrenceProperty != null) {
                var parts = recurrenceProperty.Value.Split(';').ToDictionary(x => x.Split('=').First(), x => x.Split('=').Last(), StringComparer.OrdinalIgnoreCase);
                if (parts.ContainsKey("BYDAY") && parts.ContainsKey("BYMONTH")) {
                    var month = Int32.Parse(parts["BYMONTH"]);
                    var week = Int32.Parse(parts["BYDAY"].Trim("MOTUWETHFRSASU".Distinct().ToArray()));
                    var weekdays = new Dictionary<string, DayOfWeek> {
                        {"MO", DayOfWeek.Monday},
                        {"TU", DayOfWeek.Tuesday},
                        {"WE", DayOfWeek.Wednesday},
                        {"TH", DayOfWeek.Thursday},
                        {"FR", DayOfWeek.Friday},
                        {"SA", DayOfWeek.Saturday},
                        {"SU", DayOfWeek.Sunday},
                    };
                    TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(observance.Start.TimeOfDay.Ticks), month, week < 0 ? (6 + week) : week, weekdays[parts["BYDAY"].Substring(parts["BYDAY"].Length - 2, 2)]);
                }
            }

            var nameProperty = component.Properties[PropertyName.TimeZoneName].FirstOrDefault();
            if (nameProperty != null) {
                observance.Name = nameProperty.Value;
            }

            return observance;
        }

        private static TimeSpan DeserializeTimeSpan(Property property)
        {
            if (property.Value.StartsWith("-")) {
                return TimeSpan.ParseExact(property.Value.Substring(1), "hhmm", CultureInfo.InvariantCulture, TimeSpanStyles.AssumeNegative);
            }
            if (property.Value.StartsWith("+")) {
                return TimeSpan.ParseExact(property.Value.Substring(1), "hhmm", CultureInfo.InvariantCulture);
            }
            return TimeSpan.ParseExact(property.Value, "hhmm", CultureInfo.InvariantCulture);
        }

        private static DateTime DeserializeLocalDateTime(Property property)
        {
            var typeParameter = property.Parameters[ParameterName.ValueDataType].FirstOrDefault();
            if (typeParameter != null) {
                var type = typeParameter.Values.SingleOrDefault();
                if (String.Equals("DATE", type)) {
                    return DateTime.ParseExact(property.Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                }
            }
            return DateTime.ParseExact(property.Value, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
        }

        private static DateTime DeserializeUtcDateTime(Property property)
        {
            var typeParameter = property.Parameters[ParameterName.ValueDataType].FirstOrDefault();
            if (typeParameter != null) {
                var type = typeParameter.Values.SingleOrDefault();
                if (String.Equals("DATE", type)) {
                    return DateTime.ParseExact(property.Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                }
            }
            return DateTime.ParseExact(property.Value, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        protected virtual void DeserializeEventComponent(Component component, Calendar calendar, SerializationContext context)
        {
            var @event = CreateEvent();
            DeserializeEventDateTimeStamp(@event, component);
            DeserializeEventId(@event, component);
            DeserializeEventStart(@event, component, context);
            DeserializeEventClassification(@event, component);
            DeserializeEventCreationDate(@event, component);
            DeserializeEventDescription(@event, component);
            DeserializeEventGeo(@event, component);
            DeserializeEventLastUpdate(@event, component);
            DeserializeEventLocation(@event, component);
            DeserializeEventOrganizer(@event, component);
            DeserializeEventPriority(@event, component);
            DeserializeEventSequence(@event, component);
            DeserializeEventStatus(@event, component);
            DeserializeEventSummary(@event, component);
            DeserializeEventTransparency(@event, component);
            DeserializeEventUrl(@event, component);
            DeserializeEventRecurrenceId(@event, component);
            DeserializeEventRecurrenceRule(@event, component);
            DeserializeEventEnd(@event, component, context);
            DeserializeEventDuration(@event, component);
            DeserializeEventAttachments(@event, component);
            DeserializeEventAttendees(@event, component);
            DeserializeEventCategories(@event, component);
            DeserializeEventComments(@event, component);
            DeserializeEventContacts(@event, component);
            DeserializeEventExceptionDates(@event, component);
            DeserializeEventRequestStatus(@event, component);
            DeserializeEventRelatedTo(@event, component);
            DeserializeEventResources(@event, component);
            DeserializeEventRecurrenceDateTimes(@event, component);
            DeserializeEventNonStandardProperties(@event, component);
            DeserializeEventIanaProperties(@event, component);
            calendar.AddEvent(@event);
        }

        protected virtual Event CreateEvent()
        {
            return new Event();
        }

        protected virtual void DeserializeEventDateTimeStamp(Event @event, Component component)
        {
            var property = component.Properties[PropertyName.DateTimeStamp].SingleOrDefault();
            if (property == null) {
                throw new ArgumentException("The date-time stamp is a required event property.");
            }
            // Property is not used, only validated
        }

        protected virtual void DeserializeEventId(Event @event, Component component)
        {
            var property = component.Properties[PropertyName.UniqueIdentifier].SingleOrDefault();
            if (property == null) {
                throw new ArgumentException("The unique identifier is a required event property.");
            }
            @event.Id = property.Value;
        }

        protected virtual void DeserializeEventStart(Event @event, Component component, SerializationContext context)
        {
            var property = component.Properties[PropertyName.DateTimeStart].SingleOrDefault();
            if (property == null) {
                throw new ArgumentException("The date-time start is a required event property");
            }
            @event.Start = DeserializeZonedDateTime(property, context);
        }

        private static ZonedDateTime<BclTimeZone> DeserializeZonedDateTime(Property property, SerializationContext context)
        {
            var idParameter = property.Parameters[ParameterName.TimeZoneIdentifier].SingleOrDefault();
            if (idParameter != null) {
                return new ZonedDateTime<BclTimeZone>(context.TimeZones[idParameter.Values.Single()], DeserializeLocalDateTime(property));
            }
            return new ZonedDateTime<BclTimeZone>(BclTimeZone.Utc, DeserializeUtcDateTime(property));
        }

        protected virtual void DeserializeEventClassification(Event @event, Component component)
        {
            var property = component.Properties[PropertyName.Classification].SingleOrDefault();
            if (property != null) {
                @event.Classification = Classification.FromString(property.Value);
            }
        }

        protected virtual void DeserializeEventCreationDate(Event @event, Component component)
        {
            var property = component.Properties[PropertyName.Created].SingleOrDefault();
            if (property != null) {
                @event.CreatedAt = DeserializeUtcDateTime(property);
            }
        }

        protected virtual void DeserializeEventDescription(Event @event, Component component)
        {
            @event.Description = DeserializeLocalizedString(PropertyName.Description, component);
        }

        protected virtual void DeserializeEventGeo(Event @event, Component component) {}

        protected virtual void DeserializeEventLastUpdate(Event @event, Component component)
        {
            var property = component.Properties[PropertyName.LastModified].SingleOrDefault();
            if (property != null) {
                @event.UpdatedAt = DeserializeUtcDateTime(property);
            }
        }

        protected virtual void DeserializeEventLocation(Event @event, Component component)
        {
            @event.Location = DeserializeLocalizedString(PropertyName.Location, component);
        }

        protected virtual void DeserializeEventOrganizer(Event @event, Component component)
        {
            var property = component.Properties[PropertyName.Organizer].SingleOrDefault();
            if (property != null) {
                if (!property.Value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Event organizer value must be of type cal-address.");
                }
                var organizer = new Organizer(property.Value.Substring(7));
                var nameParameter = property.Parameters[ParameterName.CommonName].SingleOrDefault();
                var name = nameParameter?.Values.SingleOrDefault();
                if (!String.IsNullOrEmpty(name)) {
                    organizer.Name = new LocalizedString(name);
                    var languageParameter = property.Parameters[ParameterName.Language].SingleOrDefault();
                    var language = languageParameter?.Values.SingleOrDefault();
                    if (!String.IsNullOrEmpty(language)) {
                        organizer.Name.CultureInfo = new CultureInfo(language);
                    }
                }
                @event.Organizer = organizer;
            }
        }

        protected virtual void DeserializeEventPriority(Event @event, Component component)
        {
            var property = component.Properties[PropertyName.Priority].SingleOrDefault();
            if (property != null) {
                @event.Priority = Priority.FromString(property.Value);
            }
        }

        protected virtual void DeserializeEventSequence(Event @event, Component component) {}

        protected virtual void DeserializeEventStatus(Event @event, Component component) {}

        protected virtual void DeserializeEventSummary(Event @event, Component component)
        {
            @event.Summary = DeserializeLocalizedString(PropertyName.Summary, component);
        }

        protected virtual void DeserializeEventTransparency(Event @event, Component component) {}

        protected virtual void DeserializeEventUrl(Event @event, Component component) {}

        protected virtual void DeserializeEventRecurrenceId(Event @event, Component component) {}

        protected virtual void DeserializeEventRecurrenceRule(Event @event, Component component) {}

        protected virtual void DeserializeEventEnd(Event @event, Component component, SerializationContext context)
        {
            var property = component.Properties[PropertyName.DateTimeEnd].FirstOrDefault();
            if (property != null) {
                if (component.Properties[PropertyName.Duration].Any()) {
                    throw new ArgumentException("The event duration and date time end properties cannot appear both in an event component.");
                }
                @event.End = DeserializeZonedDateTime(property, context);
            }
        }

        protected virtual void DeserializeEventDuration(Event @event, Component component)
        {
            var property = component.Properties[PropertyName.Duration].FirstOrDefault();
            if (property != null) {
                if (component.Properties[PropertyName.DateTimeEnd].Any()) {
                    throw new ArgumentException("The event duration and date time end properties cannot appear both in an event component.");
                }
                // TODO
                throw new NotImplementedException();
            }
        }

        protected virtual void DeserializeEventAttachments(Event @event, Component component) {}

        protected virtual void DeserializeEventAttendees(Event @event, Component component)
        {
            var properties = component.Properties[PropertyName.Attendee];
            foreach (var property in properties) {
                if (!property.Value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) {
                    throw new ArgumentException("Event attendees must be of type cal-address.");
                }
                var attendee = new Attendee(property.Value.Substring(7));
                var nameProperty = property.Parameters[ParameterName.CommonName].SingleOrDefault();
                var name = nameProperty?.Values.SingleOrDefault();
                if (!String.IsNullOrEmpty(name)) {
                    attendee.Name = new LocalizedString(name);
                    var languageParameter = property.Parameters[ParameterName.Language].SingleOrDefault();
                    var language = languageParameter?.Values.SingleOrDefault();
                    if (!String.IsNullOrEmpty(language)) {
                        attendee.Name.CultureInfo = new CultureInfo(language);
                    }
                }

                var typeParameter = property.Parameters[ParameterName.CalendarUserType].SingleOrDefault();
                var type = typeParameter?.Values.SingleOrDefault();
                if (!String.IsNullOrEmpty(type)) {
                    attendee.Type = AttendeeType.FromString(type);
                }

                var roleParameter = property.Parameters[ParameterName.ParticipationRole].SingleOrDefault();
                var role = roleParameter?.Values.SingleOrDefault();
                if (!String.IsNullOrEmpty(role)) {
                    attendee.Role = AttendeeRole.FromString(role);
                }

                var participationStatusParameter = property.Parameters[ParameterName.ParticipationStatus].SingleOrDefault();
                var participationStatus = participationStatusParameter?.Values.SingleOrDefault();
                if (!String.IsNullOrEmpty(participationStatus)) {
                    attendee.ParticipationStatus = ParticipationStatus.FromString(participationStatus);
                }

                var responseExpectedParameter = property.Parameters[ParameterName.RsvpExpectation].SingleOrDefault();
                var responseExpected = responseExpectedParameter?.Values.SingleOrDefault();
                if (!String.IsNullOrEmpty(responseExpected)) {
                    bool expected;
                    if (Boolean.TryParse(responseExpected, out expected)) {
                        attendee.ResponseExpected = expected;
                    }
                }

                @event.AddAttendee(attendee);
            }
        }

        protected virtual void DeserializeEventCategories(Event @event, Component component) {}

        protected virtual void DeserializeEventComments(Event @event, Component component) {}

        protected virtual void DeserializeEventContacts(Event @event, Component component) {}

        protected virtual void DeserializeEventExceptionDates(Event @event, Component component) {}

        protected virtual void DeserializeEventRequestStatus(Event @event, Component component) {}

        protected virtual void DeserializeEventRelatedTo(Event @event, Component component) {}

        protected virtual void DeserializeEventResources(Event @event, Component component) {}

        protected virtual void DeserializeEventRecurrenceDateTimes(Event @event, Component component) {}

        protected virtual void DeserializeEventNonStandardProperties(Event @event, Component component) {}

        protected virtual void DeserializeEventIanaProperties(Event @event, Component component) {}

        private static Property SerializeLocalizedString(string propertyName, LocalizedString localizedString)
        {
            var property = new Property(propertyName) {Value = localizedString.Text};
            if (localizedString.CultureInfo != null) {
                property.AddParameter(new Parameter(ParameterName.Language, localizedString.CultureInfo.Name));
            }
            return property;
        }

        private static LocalizedString DeserializeLocalizedString(string propertyName, Component component)
        {
            var property = component.Properties[propertyName].SingleOrDefault();
            if (property != null) {
                var languageParameter = property.Parameters[ParameterName.Language].SingleOrDefault();
                if (languageParameter != null && languageParameter.Values.Count() == 1) {
                    return new LocalizedString(property.Value, new CultureInfo(languageParameter.Values.First()));
                }
                return new LocalizedString(property.Value);
            }
            return null;
        }
    }

    public class SerializationContext : Calendar
    {
        internal IDictionary<string, BclTimeZone> TimeZones { get; } = new Dictionary<string, BclTimeZone>(StringComparer.OrdinalIgnoreCase);

    }

    internal class TimeZoneObservance
    {
        internal DateTime Start { get; set; }

        internal DateTime? End { get; set; }

        internal TimeSpan OffsetFrom { get; set; }

        internal TimeSpan OffsetTo { get; set; }

        internal string Name { get; set; }

        internal TimeZoneInfo.TransitionTime TransitionTime { get; set; }
    }
}