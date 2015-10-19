namespace Almanac.Model
{
    public class Classification : PropertyValue<string>
    {
        public static Classification Public = new Classification("PUBLIC");
        public static Classification Private = new Classification("PRIVATE");
        public static Classification Confidential = new Classification("CONFIDENTIAL");

        private Classification(string value) 
            : base(value) {}
    }
}