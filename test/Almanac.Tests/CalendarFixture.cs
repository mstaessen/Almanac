using System.Collections.Generic;
using System.IO;
using Almanac.Model;
using Almanac.Serialization;
using Almanac.Serialization.Abstractions;

namespace Almanac.Tests
{
    public abstract class CalendarFixture
    {
        public List<Calendar> Calendars { get; private set; }

        protected CalendarFixture()
        {
            Calendars = new List<Calendar>();
            var serializer = new CalendarSerializer();
            using (var stream = new FileStream("Fixtures/" + GetType().Name + ".ics", FileMode.Open)) {
                using(var reader = new StreamReader(stream)) {
                    foreach (var component in serializer.Deserialize(reader)) {
                        Calendars.Add(component);
                    }
                }
            }
        }
    }
}