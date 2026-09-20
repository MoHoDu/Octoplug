using System;
using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public sealed class RequiredExperienceTable
    {
        private readonly Dictionary<int, int> _requiredByRoomCount;

        public RequiredExperienceTable(IReadOnlyList<RequiredExperienceEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                throw new ArgumentException("At least one required EXP entry is required.", nameof(entries));
            }

            _requiredByRoomCount = new Dictionary<int, int>(entries.Count);
            foreach (var entry in entries)
            {
                if (!_requiredByRoomCount.TryAdd(entry.RoomCount, entry.RequiredExperience))
                {
                    throw new ArgumentException(
                        $"Duplicate required EXP entry for room count {entry.RoomCount}.",
                        nameof(entries));
                }
            }
        }

        public int GetRequiredExperience(int roomCount)
        {
            if (!_requiredByRoomCount.TryGetValue(roomCount, out var required))
            {
                throw new InvalidOperationException(
                    $"Required EXP is not configured for room count {roomCount}.");
            }

            return required;
        }
    }
}
