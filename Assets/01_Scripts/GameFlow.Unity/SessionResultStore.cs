using Octoplug.GameFlow;
using UnityEngine;

namespace Octoplug.GameFlow.Unity
{
    public static class SessionResultStore
    {
        private static SessionResultSnapshot snapshot;
        private static bool hasSnapshot;

        public static void Publish(SessionResultSnapshot value)
        {
            snapshot = value;
            hasSnapshot = true;
        }

        public static bool TryGet(out SessionResultSnapshot value)
        {
            value = snapshot;
            return hasSnapshot;
        }

        public static void Clear()
        {
            snapshot = default;
            hasSnapshot = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Clear();
        }
    }
}
