namespace Almanac.Model
{
    public abstract class PropertyValue<TValue>
    {
        public TValue Value { get; }

        protected PropertyValue(TValue value)
        {
            Value = value;
        }
    }
}