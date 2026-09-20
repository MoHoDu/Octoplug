using System;

namespace Octoplug.ResidentDemand
{
    public readonly struct RequiredExperienceEntry
    {
        public RequiredExperienceEntry(int roomCount, int requiredExperience)
        {
            if (roomCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roomCount));
            }

            if (requiredExperience < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredExperience));
            }

            RoomCount = roomCount;
            RequiredExperience = requiredExperience;
        }

        public int RoomCount { get; }
        public int RequiredExperience { get; }
    }
}
