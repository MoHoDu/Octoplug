using UnityEditor;
using UnityEngine;

namespace Octoplug.Power.Editor
{
    [CustomEditor(typeof(PowerStrip))]
    public class PowerStripEditor : UnityEditor.Editor
    {
        private MessageType resultType = MessageType.Info;
        private string resultMessage;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var strip = (PowerStrip)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField(
                    "Active Socket Count",
                    Application.isPlaying ? strip.ActiveSocketCount : strip.InitialSocketCount);
                EditorGUILayout.FloatField(
                    "Current Allowed Power",
                    Application.isPlaying ? strip.AllowedPowerWatts : strip.InitialAllowedPowerWatts);
                EditorGUILayout.FloatField(
                    "Current Cable Length",
                    strip.Cable != null ? strip.Cable.CableLength : 0f);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Test / Debug — Play Mode Only",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Upgrade Socket Count +1"))
                {
                    var success = strip.TryUpgradeActiveSocketCount(out var failure);
                    SetResult(success, "Socket Count", failure);
                }

                if (GUILayout.Button("Upgrade Allowed Power +1"))
                {
                    var success = strip.TryUpgradeAllowedPowerWatts(out var failure);
                    SetResult(success, "Allowed Power", failure);
                }

                using (new EditorGUI.DisabledScope(strip.Cable == null))
                {
                    if (GUILayout.Button("Upgrade Cable Length +1"))
                    {
                        var success = strip.Cable.TryUpgradeCableLength(out var failure);
                        SetResult(success, "Cable Length", failure);
                    }
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to run runtime-only upgrade controls.",
                    MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(resultMessage))
            {
                EditorGUILayout.HelpBox(resultMessage, resultType);
            }
        }

        private void SetResult<TFailure>(
            bool success,
            string label,
            TFailure failure)
        {
            resultType = success ? MessageType.Info : MessageType.Warning;
            resultMessage = success
                ? $"{label} upgraded successfully."
                : $"{label} upgrade rejected: {failure}.";
            Repaint();
        }
    }
}
