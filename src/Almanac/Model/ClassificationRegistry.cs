using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class ClassificationRegistry : PropertyValueRegistry<Classification, string>
    {
        static ClassificationRegistry()
        {
            Register(Classification.Public);
            Register(Classification.Private);
            Register(Classification.Confidential);
        }
    }
}