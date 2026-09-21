using System;
using System.Reflection;
using NUnit.Framework;
using Octoplug.Reward;
using Octoplug.Reward.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Octoplug.Tests.Reward.Unity
{
    public class RewardUiControllerTests
    {
        private GameObject root;
        private RewardUiController view;
        private TextMeshProUGUI[] titles;
        private TextMeshProUGUI[] descriptions;
        private Button[] buttons;
        private Button passButton;

        [SetUp]
        public void Setup()
        {
            root = new GameObject("Reward UI");
            var dim = new GameObject("Dim");
            var choices = new GameObject("Choices");
            dim.transform.SetParent(root.transform);
            choices.transform.SetParent(root.transform);

            titles = new TextMeshProUGUI[3];
            descriptions = new TextMeshProUGUI[3];
            buttons = new Button[3];
            for (var index = 0; index < 3; index++)
            {
                var choice = new GameObject($"Choice{index + 1}");
                choice.transform.SetParent(choices.transform);
                titles[index] = CreateText(choice.transform, "Title");
                descriptions[index] = CreateText(choice.transform, "Description");
                buttons[index] = CreateButton(choice.transform, "Button");
            }

            passButton = CreateButton(root.transform, "PassButton");
            view = root.AddComponent<RewardUiController>();
            SetField(view, "dim", dim);
            SetField(view, "choices", choices);
            SetField(view, "passButton", passButton);
            ConfigureSlots(view);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void Show_BindsDisplayTextAndExactReward_ThenLocksAllButtons()
        {
            var rewards = CreateRewards("A");
            RewardBalanceRecord selected = null;

            Assert.That(view.Show(rewards, reward => selected = reward, () => { }), Is.True);
            for (var index = 0; index < 3; index++)
            {
                Assert.That(titles[index].text, Is.EqualTo(rewards[index].DisplayName));
                Assert.That(descriptions[index].text, Is.EqualTo(rewards[index].Description));
                Assert.That(titles[index].text, Does.Not.Contain(rewards[index].Id));
            }

            buttons[1].onClick.Invoke();
            buttons[2].onClick.Invoke();

            Assert.That(selected, Is.SameAs(rewards[1]));
            Assert.That(buttons, Has.All.Matches<Button>(button => !button.interactable));
            Assert.That(passButton.interactable, Is.False);
        }

        [Test]
        public void Pass_IsOneShotAndDoesNotSelectReward()
        {
            var selectedCount = 0;
            var passCount = 0;
            view.Show(CreateRewards("A"), _ => selectedCount++, () => passCount++);

            passButton.onClick.Invoke();
            passButton.onClick.Invoke();
            buttons[0].onClick.Invoke();

            Assert.That(passCount, Is.EqualTo(1));
            Assert.That(selectedCount, Is.Zero);
        }

        [Test]
        public void HideAndSecondCycle_RemoveStaleCallbacksAndRecords()
        {
            var firstCount = 0;
            var second = CreateRewards("B");
            RewardBalanceRecord selected = null;
            view.Show(CreateRewards("A"), _ => firstCount++, () => { });
            view.Hide();

            Assert.That(root.activeSelf, Is.False);
            Assert.That(titles, Has.All.Matches<TextMeshProUGUI>(text => text.text == string.Empty));

            view.Show(second, reward => selected = reward, () => { });
            buttons[2].onClick.Invoke();

            Assert.That(firstCount, Is.Zero);
            Assert.That(selected, Is.SameAs(second[2]));
        }

        private void ConfigureSlots(RewardUiController controller)
        {
            var slotType = typeof(RewardUiController).GetNestedType("ChoiceSlot", BindingFlags.NonPublic);
            var slots = Array.CreateInstance(slotType, 3);
            for (var index = 0; index < 3; index++)
            {
                var slot = Activator.CreateInstance(slotType, true);
                SetField(slot, "title", titles[index]);
                SetField(slot, "description", descriptions[index]);
                SetField(slot, "button", buttons[index]);
                slots.SetValue(slot, index);
            }
            SetField(controller, "choiceSlots", slots);
        }

        private static RewardBalanceRecord[] CreateRewards(string prefix)
        {
            var rewards = new RewardBalanceRecord[3];
            for (var index = 0; index < rewards.Length; index++)
            {
                rewards[index] = new RewardBalanceRecord(
                    $"{prefix}-internal-{index}",
                    true,
                    1,
                    1,
                    $"Display {prefix}{index}",
                    $"Description {prefix}{index}",
                    RewardTargetType.None,
                    new[] { new RewardEffect(RewardEffectType.HouseAllowedPower, 1) });
            }
            return rewards;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static Button CreateButton(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent);
            gameObject.AddComponent<Image>();
            return gameObject.AddComponent<Button>();
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
