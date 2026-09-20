using System;
using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.ResidentDemand.Unity
{
    [CreateAssetMenu(
        fileName = "SessionProgressConfig",
        menuName = "Octoplug/Resident Demand/Session Progress Config")]
    public sealed class SessionProgressConfig : ScriptableObject
    {
        [SerializeField]
        [Range(0, 100)]
        private int initialGlobalSatisfaction = 100;

        [SerializeField]
        private List<RequiredExperienceValue> requiredExperience = new();

        public int InitialGlobalSatisfaction => initialGlobalSatisfaction;

        public RequiredExperienceTable CreateRequiredExperienceTable()
        {
            var entries = new RequiredExperienceEntry[requiredExperience.Count];
            for (var index = 0; index < requiredExperience.Count; index++)
            {
                entries[index] = new RequiredExperienceEntry(
                    requiredExperience[index].RoomCount,
                    requiredExperience[index].RequiredExperience);
            }

            return new RequiredExperienceTable(entries);
        }

        [Serializable]
        private struct RequiredExperienceValue
        {
            [SerializeField]
            private int roomCount;

            [SerializeField]
            private int requiredExperience;

            public int RoomCount => roomCount;
            public int RequiredExperience => requiredExperience;
        }
    }
}
