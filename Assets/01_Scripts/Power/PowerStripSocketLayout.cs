using System;
using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// PowerStrip-only presentation data layered over the shared persistent
    /// Socket module layout. PowerInfo positions remain authored and independent
    /// of the shared activation and collider rules.
    /// </summary>
    public class PowerStripSocketLayout : MonoBehaviour
    {
        [Serializable]
        private class PowerInfoPosition
        {
            [SerializeField]
            private bool authored;

            [SerializeField]
            private Vector3 localPosition;

            public bool Authored => authored;
            public Vector3 LocalPosition => localPosition;
        }

        [SerializeField]
        private SocketModuleLayout moduleLayout;

        [SerializeField]
        private Transform powerInfo;

        [SerializeField]
        private List<PowerInfoPosition> powerInfoPositions = new();

        public SocketModuleLayout ModuleLayout => moduleLayout;

        public bool TryValidate(int socketCount)
        {
            if (moduleLayout == null
                || !moduleLayout.TryValidate(socketCount)
                || powerInfo == null
                || powerInfoPositions.Count != moduleLayout.Capacity)
            {
                return false;
            }

            for (var i = 0; i < powerInfoPositions.Count; i++)
            {
                if (powerInfoPositions[i] == null
                    || !powerInfoPositions[i].Authored)
                {
                    return false;
                }
            }

            return true;
        }

        public void Apply(int socketCount)
        {
            moduleLayout.Apply(socketCount);
            powerInfo.localPosition =
                powerInfoPositions[socketCount - 1].LocalPosition;
        }
    }
}
