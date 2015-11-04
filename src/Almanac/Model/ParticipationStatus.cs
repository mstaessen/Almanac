using System.Text;
using Almanac.Model.Abstractions;

namespace Almanac.Model
{
    public class ParticipationStatus : PropertyValue
    {
        private static readonly PropertyValueRegistry<ParticipationStatus> Registry = new PropertyValueRegistry<ParticipationStatus>();


        public static ParticipationStatus NeedsAction { get; } = FromString("NEEDS-ACTION");
        public static ParticipationStatus Accepted { get; } = FromString("ACCEPTED");
        public static ParticipationStatus Declined { get; } = FromString("DECLINED");
        public static ParticipationStatus Tentative { get; } = FromString("TENTATIVE");
        public static ParticipationStatus Delegated { get; } = FromString("DELEGATED");
        public static ParticipationStatus Completed { get; } = FromString("COMPLETED");
        public static ParticipationStatus InProcess { get; } = FromString("IN-PROCESS");

        public static ParticipationStatus FromString(string value)
        {
            return Registry.FromString(value);
        }
    }
}