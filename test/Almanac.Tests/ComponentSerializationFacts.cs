using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Almanac.Serialization;
using Almanac.Serialization.Abstractions;
using Xunit;

namespace Almanac.Tests
{
    public class ComponentSerializationFacts
    {
        [Fact]
        public void SerializerCanRoundTripComponents()
        {
            // Arrange
            var parameter = new Parameter("X-MY-PARAMETER");
            parameter.AddValue("One");
            parameter.AddValue(",");
            parameter.AddValue("Two");
            parameter.AddValue(":");
            parameter.AddValue("Three");
            parameter.AddValue(";");
            var property = new Property("X-MY-PROPERTY");
            property.AddParameter(parameter);
            property.Value = "My Value";
            var innerComponent = new Component("X-MY-INNER-COMPONENT");
            innerComponent.AddProperty(property);
            var component = new Component("X-MY-COMPONENT");
            component.AddProperty(property);
            component.AddComponent(innerComponent);

            // Act
            var serializer = new ComponentSerializer();
            using (var stream = new MemoryStream()) {
                using (var writer = new StreamWriter(stream)) {
                    serializer.Serialize(component, writer);
                    Debug.WriteLine(Encoding.UTF8.GetString(stream.GetBuffer()));
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream)) {
                        var result = serializer.Deserialize(reader).FirstOrDefault();
                        // Assert
                        Assert.Equal(component, result);
                    }
                }
            }
        }
    }
}