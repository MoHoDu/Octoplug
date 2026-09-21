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

        public int InitialGlobalSatisfaction => initialGlobalSatisfaction;

        public RequiredExperienceTable CreateRequiredExperienceTable()
        {
            // The Source of Truth is now the progression balance data.
            return Octoplug.ResidentDemand.RequiredExperienceMapper.Create();
        }
    }
}
