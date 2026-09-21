using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Octoplug.GameFlow.Unity
{
    public sealed class ResultSceneController : MonoBehaviour
    {
        [SerializeField] private TMP_Text roomCountText;
        [SerializeField] private TMP_Text solvedDemandCountText;
        [SerializeField] private TMP_Text failedDemandCountText;
        [SerializeField] private Button backToLobbyButton;

        private Action returnToLobby = DemoSceneFlow.ReturnToLobby;

        private void OnEnable()
        {
            if (backToLobbyButton != null)
            {
                backToLobbyButton.onClick.AddListener(HandleBackToLobbyClicked);
            }

            BindStoredSnapshot();
        }

        private void OnDisable()
        {
            if (backToLobbyButton != null)
            {
                backToLobbyButton.onClick.RemoveListener(HandleBackToLobbyClicked);
            }
        }

        public void InitializeForVerification(
            TMP_Text roomText,
            TMP_Text solvedText,
            TMP_Text failedText,
            Button lobbyButton,
            Action lobbyRequest)
        {
            if (backToLobbyButton != null)
            {
                backToLobbyButton.onClick.RemoveListener(HandleBackToLobbyClicked);
            }

            roomCountText = roomText != null
                ? roomText
                : throw new ArgumentNullException(nameof(roomText));
            solvedDemandCountText = solvedText != null
                ? solvedText
                : throw new ArgumentNullException(nameof(solvedText));
            failedDemandCountText = failedText != null
                ? failedText
                : throw new ArgumentNullException(nameof(failedText));
            backToLobbyButton = lobbyButton != null
                ? lobbyButton
                : throw new ArgumentNullException(nameof(lobbyButton));
            returnToLobby = lobbyRequest ?? throw new ArgumentNullException(nameof(lobbyRequest));
            backToLobbyButton.onClick.AddListener(HandleBackToLobbyClicked);
        }

        public void Bind(SessionResultSnapshot snapshot)
        {
            if (roomCountText == null ||
                solvedDemandCountText == null ||
                failedDemandCountText == null)
            {
                throw new InvalidOperationException("Result text references are not assigned.");
            }

            roomCountText.text = snapshot.FinalRoomCount.ToString();
            solvedDemandCountText.text = snapshot.SolvedDemandCount.ToString();
            failedDemandCountText.text = snapshot.FailedDemandCount.ToString();
        }

        private void BindStoredSnapshot()
        {
            if (!SessionResultStore.TryGet(out var snapshot))
            {
                Debug.LogError("Result Scene opened without a Session result snapshot.", this);
                return;
            }

            Bind(snapshot);
        }

        private void HandleBackToLobbyClicked()
        {
            returnToLobby();
        }
    }
}
