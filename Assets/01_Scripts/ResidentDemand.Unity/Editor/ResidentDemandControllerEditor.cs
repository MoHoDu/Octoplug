using UnityEditor;
using UnityEngine;

namespace Octoplug.ResidentDemand.Unity.Editor
{
    [CustomEditor(typeof(ResidentDemandController))]
    public sealed class ResidentDemandControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                var controller = (ResidentDemandController)target;
                if (GUILayout.Button("Add Resident"))
                {
                    controller.AddResidentFromInspector();
                }

                if (GUILayout.Button("Refresh Demand Availability"))
                {
                    controller.RefreshDemandAvailabilityFromInspector();
                }

                if (GUILayout.Button("Inspect Demand State"))
                {
                    controller.InspectDemandStateFromInspector();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Debug controls are available in Play Mode.",
                    MessageType.Info);
            }
        }
    }
}
