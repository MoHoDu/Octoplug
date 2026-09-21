using Octoplug.RoomGeneration.Unity;
using TMPro;
using UnityEngine;

namespace Octoplug.GameFlow.Unity
{
    public class RoomCountHudBinder : MonoBehaviour
    {
        [SerializeField] private ProductionRoomGenerationController roomGeneration;
        [SerializeField] private TextMeshProUGUI countText;

        private int lastCount = -1;

        private void Update()
        {
            if (roomGeneration != null && roomGeneration.IsInitialized && countText != null)
            {
                int currentCount = roomGeneration.State.UnlockedLayout.Rooms.Count;
                if (currentCount != lastCount)
                {
                    lastCount = currentCount;
                    countText.text = currentCount.ToString();
                }
            }
        }
    }
}
