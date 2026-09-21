using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Octoplug.Reward.Unity
{
    public class RewardUiController : MonoBehaviour
    {
        [Serializable]
        private sealed class ChoiceSlot
        {
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI description;
            [SerializeField] private Button button;

            public Button Button => button;

            public void Bind(RewardBalanceRecord reward, Action onClick)
            {
                title.text = reward.DisplayName;
                description.text = reward.Description;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick());
                button.interactable = true;
            }

            public void Clear()
            {
                if (title != null)
                {
                    title.text = string.Empty;
                }

                if (description != null)
                {
                    description.text = string.Empty;
                }

                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.interactable = true;
                }
            }

            public bool IsConfigured => title != null && description != null && button != null;
        }

        [SerializeField] private GameObject dim;
        [SerializeField] private GameObject choices;
        [SerializeField] private ChoiceSlot[] choiceSlots = new ChoiceSlot[3];
        [SerializeField] private Button passButton;

        private readonly RewardBalanceRecord[] boundRewards = new RewardBalanceRecord[3];
        private Action<RewardBalanceRecord> onRewardSelected;
        private Action onPass;
        private bool inputConsumed;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            ResetState();
        }

        private void OnDisable()
        {
            ResetState();
        }

        public bool Show(
            IReadOnlyList<RewardBalanceRecord> rewards,
            Action<RewardBalanceRecord> rewardSelected,
            Action pass)
        {
            if (!IsConfigured() || rewards == null || rewards.Count != 3)
            {
                Debug.LogError("Reward UI requires exactly three rewards and complete authored bindings.", this);
                return false;
            }

            gameObject.SetActive(true);
            ResetState();
            onRewardSelected = rewardSelected;
            onPass = pass;

            for (var index = 0; index < choiceSlots.Length; index++)
            {
                var slotIndex = index;
                var reward = rewards[index];
                boundRewards[index] = reward;
                choiceSlots[index].Bind(reward, () => SelectReward(slotIndex));
            }

            passButton.onClick.RemoveAllListeners();
            passButton.onClick.AddListener(Pass);
            passButton.interactable = true;
            dim.SetActive(true);
            choices.SetActive(true);
            gameObject.SetActive(true);
            return true;
        }

        public void Hide()
        {
            ResetState();
            gameObject.SetActive(false);
        }

        private void SelectReward(int index)
        {
            if (!TryConsumeInput())
            {
                return;
            }

            var reward = boundRewards[index];
            Debug.Log($"[RewardUiController] Clicked choice {index} -> Selected Reward ID: {reward.Id} (TargetType: {reward.TargetType})", this);
            onRewardSelected?.Invoke(reward);
        }

        private void Pass()
        {
            if (!TryConsumeInput())
            {
                return;
            }

            Debug.Log("[RewardUiController] Clicked PassButton -> Selected Pass", this);
            onPass?.Invoke();
        }

        private bool TryConsumeInput()
        {
            if (inputConsumed)
            {
                return false;
            }

            inputConsumed = true;
            SetButtonsInteractable(false);
            return true;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            for (var index = 0; index < choiceSlots.Length; index++)
            {
                if (choiceSlots[index]?.Button != null)
                {
                    choiceSlots[index].Button.interactable = interactable;
                }
            }

            if (passButton != null)
            {
                passButton.interactable = interactable;
            }
        }

        private bool IsConfigured()
        {
            if (dim == null || choices == null || passButton == null
                || choiceSlots == null || choiceSlots.Length != 3)
            {
                return false;
            }

            for (var index = 0; index < choiceSlots.Length; index++)
            {
                if (choiceSlots[index] == null || !choiceSlots[index].IsConfigured)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResetState()
        {
            inputConsumed = false;
            onRewardSelected = null;
            onPass = null;

            if (choiceSlots != null)
            {
                for (var index = 0; index < choiceSlots.Length; index++)
                {
                    choiceSlots[index]?.Clear();
                }
            }

            Array.Clear(boundRewards, 0, boundRewards.Length);
            if (passButton != null)
            {
                passButton.onClick.RemoveAllListeners();
                passButton.interactable = true;
            }
        }
    }
}
