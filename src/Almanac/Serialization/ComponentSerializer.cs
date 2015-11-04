using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Almanac.Serialization.Abstractions;

namespace Almanac.Serialization
{
    public class ComponentSerializer
    {
        public void Serialize(Component component, TextWriter writer)
        {
            WriteComponent(component, writer);
            writer.Flush();
        }

        private void WriteComponent(Component component, TextWriter writer)
        {
            writer.Write(Token.Begin);
            writer.Write(Token.Colon);
            writer.Write(component.Name);
            writer.Write(Token.NewLine);
            foreach (var group in component.Properties) {
                foreach (var property in group) {
                    WriteProperty(property, writer);
                    writer.Write(Token.NewLine);
                }
            }
            foreach (var group in component.Components) {
                foreach (var innerComponent in group) {
                    WriteComponent(innerComponent, writer);
                    writer.Write(Token.NewLine);
                }
            }
            writer.Write(Token.End);
            writer.Write(Token.Colon);
            writer.Write(component.Name);
        }

        private void WriteProperty(Property property, TextWriter writer)
        {
            // TODO Implement line folding!
            writer.Write(property.Name);
            foreach (var group in property.Parameters) {
                foreach (var parameter in group) {
                    writer.Write(Token.Semicolon);
                    WriteParameter(parameter, writer);
                }
            }
            writer.Write(Token.Colon);
            writer.Write(property.Value);
        }

        private void WriteParameter(Parameter parameter, TextWriter writer)
        {
            writer.Write(parameter.Name);
            writer.Write(Token.EqualSign);
            writer.Write(String.Join(Token.Comma.ToString(), parameter.Values.Select(x => parameter.Quoted
                || x.Contains(Token.Semicolon)
                || x.Contains(Token.Colon)
                || x.Contains(Token.Comma)
                    ? $"{Token.DoubleQuote}{x}{Token.DoubleQuote}" : x)));
        }

        public IEnumerable<Component> Deserialize(string input)
        {
            return Deserialize(SplitLines(input));
        }

        public IEnumerable<Component> Deserialize(TextReader reader)
        {
            return Deserialize(ReadLines(reader));
        }

        private IEnumerable<Component> Deserialize(IEnumerable<string> lines)
        {
            var componentStack = new Stack<Component>();
            var property = new StringBuilder();
            foreach (var line in lines) {
                // If line starts with a space or tab, unfold line...
                if (line.StartsWith(Token.Space.ToString(), StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith(Token.Tab.ToString(), StringComparison.OrdinalIgnoreCase)) {
                    property.Append(line.Substring(1));
                }
                // Line does not start with space or tab
                else {
                    // Lex unfolded content line and add it to the current component
                    if (property.Length > 0) {
                        if (!componentStack.Any()) {
                            throw new InvalidOperationException("Properties should only appear in components.");
                        }
                        componentStack.Peek().AddProperty(CreateProperty(property.ToString()));
                        property.Clear();
                    }

                    // Line starts with 'BEGIN'
                    if (line.StartsWith(Token.Begin, StringComparison.OrdinalIgnoreCase)) {
                        var name = line.Substring(6);
                        var component = new Component(name);
                        // Not the root component
                        if (componentStack.Any()) {
                            componentStack.Peek().AddComponent(component);
                        }
                        componentStack.Push(component);
                    }
                    // Line starts with 'END'
                    else if (line.StartsWith(Token.End, StringComparison.OrdinalIgnoreCase)) {
                        var name = line.Substring(4);
                        if (!componentStack.Any() && !String.Equals(componentStack.Peek().Name, name, StringComparison.OrdinalIgnoreCase)) {
                            throw new InvalidOperationException("Invalid nesting");
                        }
                        var component = componentStack.Pop();
                        if (!componentStack.Any()) {
                            yield return component;
                        }
                    }
                    // None of the above, this must be the beginning of property
                    else {
                        property.Append(line);
                    }
                }
            }
        }

        private Property CreateProperty(string contentLine)
        {
            using (var propertySerializer = new PropertySerializer(contentLine)) {
                return propertySerializer.Deserialize();
            }
        }

        private IEnumerable<string> SplitLines(string input)
        {
            return input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        private IEnumerable<string> ReadLines(TextReader reader)
        {
            var line = reader.ReadLine();
            while (line != null) {
                yield return line;
                line = reader.ReadLine();
            }
        }

        private class PropertySerializer : IDisposable
        {
            private readonly StringReader reader;

            public PropertySerializer(string contentLine)
            {
                reader = new StringReader(contentLine);
            }

            public Property Deserialize()
            {
                var property = new Property(ReadName());
                foreach (var parameter in ReadParameters()) {
                    property.AddParameter(parameter);
                }
                property.Value = ReadValue();
                return property;
            }

            private string ReadName()
            {
                return ReadUntil(new [] {Token.Semicolon, Token.Colon});
            }

            private IEnumerable<Parameter> ReadParameters()
            {
                while (reader.Peek() != -1 && Convert.ToChar(reader.Peek()) == Token.Semicolon) {
                    reader.Read();
                    yield return ReadParameter();
                }
            }

            private Parameter ReadParameter()
            {
                var parameter = new Parameter(ReadParameterName());
                if (Convert.ToChar(reader.Read()) != Token.EqualSign) {
                    throw new InvalidOperationException("Property parameter syntax is invalid.");
                }
                parameter.AddValue(ReadParameterValue());
                foreach (var value in ReadParameterValues()) {
                    parameter.AddValue(value);
                }
                return parameter;
            }

            private string ReadParameterName()
            {
                return ReadUntil(new [] {Token.EqualSign});
            }

            private string ReadParameterValue()
            {
                return ReadUntil(Token.DoubleQuote, new [] {Token.Comma, Token.Semicolon, Token.Colon}).Trim('"');
            }

            private IEnumerable<string> ReadParameterValues()
            {
                while (reader.Peek() != -1 && Convert.ToChar(reader.Peek()) == Token.Comma) {
                    reader.Read();
                    yield return ReadParameterValue();
                }
            }

            private string ReadValue()
            {
                if (Convert.ToChar(reader.Read()) != Token.Colon) {
                    throw new InvalidOperationException("Property syntax is invalid");
                }
                return reader.ReadToEnd();
            }

            private string ReadUntil(char[] tokens)
            {
                return ReadUntil(null, tokens);
            }

            private string ReadUntil(char? escapeChar, char[] tokens)
            {
                var buffer = new StringBuilder();
                var escaping = false;
                while (reader.Peek() != -1 && (!tokens.Contains(Convert.ToChar(reader.Peek())) || escaping)) {
                    var character = Convert.ToChar(reader.Read());
                    buffer.Append(character);
                    if (character == escapeChar) {
                        escaping = !escaping;
                    }
                }
                return buffer.ToString();
            }

            public void Dispose()
            {
                reader.Dispose();
            }
        }
    }
}