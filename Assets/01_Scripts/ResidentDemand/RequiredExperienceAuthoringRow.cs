namespace Octoplug.ResidentDemand
{
    public sealed class RequiredExperienceAuthoringRow
    {
        public RequiredExperienceAuthoringRow(int roomCount, int requiredExperience)
        {
            RoomCount = roomCount;
            RequiredExperience = requiredExperience;
        }

        public int RoomCount { get; }
        public int RequiredExperience { get; }
    }
}
