using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.ResidentDemand;
using Octoplug.ResidentDemand.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Octoplug.Tests.Editor.ResidentDemand
{
    public sealed class SessionProgressIntegrationTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (var index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void Controller_AppliesExactOutcomeAndPublishesOneShotSignals()
        {
            var demandController = Track(new GameObject("Demand Controller"))
                .AddComponent<ResidentDemandController>();
            demandController.InitializeForVerification(1);
            var progress = Track(new GameObject("Session Progress"))
                .AddComponent<SessionProgressController>();
            progress.InitializeForVerification(
                demandController,
                8,
                15,
                1,
                RequiredExperience());
            var changes = 0;
            var depleted = 0;
            var threshold = 0;
            progress.ProgressChanged += () => changes++;
            progress.SatisfactionDepleted += () => depleted++;
            progress.ExperienceThresholdReached += () => threshold++;

            progress.ApplyOutcomeForVerification(Outcome(DemandResolution.Success));
            progress.ApplyOutcomeForVerification(Outcome(DemandResolution.Success));
            progress.ApplyOutcomeForVerification(Outcome(DemandResolution.Failure));
            progress.ApplyOutcomeForVerification(Outcome(DemandResolution.Failure));
            progress.ApplyOutcomeForVerification(Outcome(DemandResolution.Failure));

            Assert.That(progress.CurrentExperience, Is.EqualTo(35));
            Assert.That(progress.GlobalSatisfaction, Is.Zero);
            Assert.That(changes, Is.EqualTo(5));
            Assert.That(threshold, Is.EqualTo(1));
            Assert.That(depleted, Is.EqualTo(1));
        }

        [Test]
        public void Config_UsesExplicitRowsAndRejectsDuplicateRoomCounts()
        {
            var config = Track(ScriptableObject.CreateInstance<SessionProgressConfig>());
            var serialized = new SerializedObject(config);
            var rows = serialized.FindProperty("requiredExperience");
            rows.arraySize = 2;
            SetRequiredExperience(rows.GetArrayElementAtIndex(0), 1, 20);
            SetRequiredExperience(rows.GetArrayElementAtIndex(1), 2, 30);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var table = config.CreateRequiredExperienceTable();

            Assert.That(config.InitialGlobalSatisfaction, Is.EqualTo(100));
            Assert.That(table.GetRequiredExperience(1), Is.EqualTo(20));
            Assert.That(table.GetRequiredExperience(2), Is.EqualTo(30));
            Assert.Throws<System.InvalidOperationException>(() =>
                table.GetRequiredExperience(3));

            serialized.Update();
            SetRequiredExperience(rows.GetArrayElementAtIndex(1), 1, 30);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.Throws<System.ArgumentException>(() =>
                config.CreateRequiredExperienceTable());
        }

        [Test]
        public void Hud_BindsNormalizedValuesAndExactSatisfactionColorBoundary()
        {
            var root = Track(new GameObject("Game Status"));
            var view = root.AddComponent<GameStatusInfoView>();
            var satisfaction = CreateSlider(root.transform, "Satisfaction");
            var experience = CreateSlider(root.transform, "Experience");
            var satisfactionFill = satisfaction.transform.Find("Fill").GetComponent<Image>();
            var authoredGreen = new Color(0.078431375f, 0.40000004f, 0.34901962f, 1f);
            var authoredOrange = new Color(0.9607844f, 0.4156863f, 0.14509805f, 1f);
            satisfactionFill.color = authoredGreen;
            var experienceFill = experience.transform.Find("Fill").GetComponent<Image>();
            experienceFill.color = authoredOrange;
            SetObjectReference(view, "satisfactionSlider", satisfaction);
            SetObjectReference(view, "satisfactionFill", satisfactionFill);
            SetObjectReference(view, "experienceSlider", experience);

            view.Bind(29, 0.29f, 1.5f);

            Assert.That(satisfaction.value, Is.EqualTo(0.29f).Within(0.0001f));
            Assert.That(satisfactionFill.color, Is.EqualTo(new Color(1f, 0f, 0.19607843f, 1f)));
            Assert.That(experience.value, Is.EqualTo(1f));
            Assert.That(experienceFill.color, Is.EqualTo(authoredOrange));

            view.Bind(30, 0.3f, 0.25f);

            Assert.That(satisfaction.value, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(satisfactionFill.color, Is.EqualTo(authoredGreen));
            Assert.That(experience.value, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(experienceFill.color, Is.EqualTo(authoredOrange));
        }

        private static RequiredExperienceTable RequiredExperience()
        {
            return new RequiredExperienceTable(new[]
            {
                new RequiredExperienceEntry(1, 20),
                new RequiredExperienceEntry(2, 30),
            });
        }

        private static DemandOutcome Outcome(DemandResolution resolution)
        {
            return new DemandOutcome(
                new DemandBalanceRecord(
                    "TEST",
                    true,
                    1,
                    1,
                    new[] { ResidentNeedType.Fun },
                    1f,
                    1f,
                    10,
                    5,
                    -8,
                    0f),
                resolution);
        }

        private static Slider CreateSlider(Transform parent, string name)
        {
            var sliderObject = new GameObject(name);
            sliderObject.transform.SetParent(parent);
            var slider = sliderObject.AddComponent<Slider>();
            var fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(sliderObject.transform);
            fillObject.AddComponent<Image>();
            return slider;
        }

        private static void SetObjectReference(
            Object target,
            string propertyName,
            Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRequiredExperience(
            SerializedProperty row,
            int roomCount,
            int requiredExperience)
        {
            row.FindPropertyRelative("roomCount").intValue = roomCount;
            row.FindPropertyRelative("requiredExperience").intValue = requiredExperience;
        }

        private T Track<T>(T target)
            where T : Object
        {
            _objects.Add(target);
            return target;
        }
    }
}
