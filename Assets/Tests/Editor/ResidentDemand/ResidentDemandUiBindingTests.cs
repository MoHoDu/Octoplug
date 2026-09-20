using System;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Power;
using Octoplug.Power.Connection;
using Octoplug.Power.UI;
using Octoplug.ResidentDemand;
using Octoplug.ResidentDemand.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Octoplug.Tests.Editor.ResidentDemand
{
    public sealed class ResidentDemandUiBindingTests
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
        public void ResidentCard_BindsOneNeedAndAuthoritativeWaitingAndUsingProgress()
        {
            var sprite = CreateSprite();
            var iconLibrary = CreateIconLibrary(sprite);
            var fixture = CreateResidentCard();
            var resident = new ResidentDemandState(new ResidentNumber(1));
            resident.StartDemand(CreateDemand());

            resident.Advance(2f);
            fixture.View.Bind(resident, iconLibrary);

            Assert.That(fixture.Number.text, Is.EqualTo("01"));
            Assert.That(fixture.Icon01.sprite, Is.SameAs(sprite));
            Assert.That(fixture.Icon01.gameObject.activeSelf, Is.True);
            Assert.That(fixture.Icon02.gameObject.activeSelf, Is.False);
            Assert.That(fixture.None.activeSelf, Is.False);
            Assert.That(fixture.Slider.value, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(fixture.Fill.color, Is.EqualTo(new Color(1f, 0f, 0.19607843f, 1f)));

            resident.SetUsing(true);
            resident.Advance(4f);
            fixture.View.Bind(resident, iconLibrary);

            Assert.That(fixture.Slider.value, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(fixture.Fill.color, Is.EqualTo(fixture.AuthoredGreen));
        }

        [Test]
        public void ResidentCard_BindsTwoNeedsAndCompletedCheck()
        {
            var needSprite = CreateSprite();
            var checkSprite = CreateSprite();
            var fixture = CreateResidentCard(checkSprite);
            var resident = new ResidentDemandState(new ResidentNumber(1));
            resident.StartDemand(CreateDemand(ResidentNeedType.Cooling, ResidentNeedType.Heating));

            var iconLibrary = CreateIconLibrary(needSprite, needSprite);
            fixture.View.Bind(resident, iconLibrary);

            Assert.That(fixture.Icon01.sprite, Is.SameAs(needSprite));
            Assert.That(fixture.Icon02.sprite, Is.SameAs(needSprite));
            Assert.That(fixture.Icon01.gameObject.activeSelf, Is.True);
            Assert.That(fixture.Icon02.gameObject.activeSelf, Is.True);

            resident.SetUsing(true);
            resident.Advance(10f);
            fixture.View.Bind(resident, iconLibrary);

            Assert.That(resident.IsNeedCompleted(0), Is.True);
            Assert.That(fixture.Icon01.sprite, Is.SameAs(checkSprite));
            Assert.That(fixture.Icon02.sprite, Is.SameAs(needSprite));
        }

        [TestCase(ResidentDemandStatus.None)]
        [TestCase(ResidentDemandStatus.Cooldown)]
        public void ResidentCard_IdleAndCooldownShowNoneWithoutChangingNumber(
            ResidentDemandStatus expectedStatus)
        {
            var fixture = CreateResidentCard();
            var resident = new ResidentDemandState(new ResidentNumber(2));
            if (expectedStatus == ResidentDemandStatus.Cooldown)
            {
                resident.StartDemand(CreateDemand(cooldownSeconds: 5f));
                resident.SetUsing(true);
                resident.Advance(10f);
            }

            Assert.That(resident.Status, Is.EqualTo(expectedStatus));
            fixture.View.Bind(resident, CreateIconLibrary(CreateSprite()));

            Assert.That(fixture.Number.text, Is.EqualTo("02"));
            Assert.That(fixture.Icon01.gameObject.activeSelf, Is.False);
            Assert.That(fixture.Icon02.gameObject.activeSelf, Is.False);
            Assert.That(fixture.None.activeSelf, Is.True);
            Assert.That(fixture.StatusInfo.activeSelf, Is.False);
        }

        [Test]
        public void Coordinator_ReusesResidentOneAndCreatesLaterCardsOnceInStableOrder()
        {
            var controllerObject = Track(new GameObject("Resident Demand Controller"));
            var controller = controllerObject.AddComponent<ResidentDemandController>();
            controller.InitializeForVerification(1);

            var residentsArea = Track(new GameObject("Residents Area"));
            var residentOne = CreateResidentCard(residentsArea.transform, "Resident 01");
            var prefab = CreateResidentCard(null, "Resident Card Prefab");
            prefab.View.gameObject.SetActive(false);
            var iconLibrary = CreateIconLibrary(CreateSprite());

            var coordinatorObject = Track(new GameObject("Resident UI Coordinator"));
            coordinatorObject.SetActive(false);
            var coordinator = coordinatorObject.AddComponent<ResidentDemandUiCoordinator>();
            SetObjectReference(coordinator, "controller", controller);
            SetObjectReference(coordinator, "residentsArea", residentsArea.transform);
            SetObjectReference(coordinator, "residentOneCard", residentOne.View);
            SetObjectReference(coordinator, "residentCardPrefab", prefab.View);
            SetObjectReference(coordinator, "usageTypeIconLibrary", iconLibrary);
            coordinatorObject.SetActive(true);

            controller.AddResidentForVerification(CreateDemand());
            controller.AddResidentForVerification(CreateDemand());
            coordinator.RefreshResidentCards();
            coordinator.RefreshResidentCards();

            Assert.That(residentsArea.transform.childCount, Is.EqualTo(2));
            Assert.That(residentsArea.transform.GetChild(0), Is.SameAs(residentOne.View.transform));
            Assert.That(
                residentsArea.transform.GetChild(0).GetComponent<ResidentCardView>(),
                Is.SameAs(residentOne.View));
            Assert.That(
                residentsArea.transform.GetChild(1).GetComponentInChildren<TMP_Text>(true).text,
                Is.EqualTo("02"));
        }

        [Test]
        public void Coordinator_AssignmentEventsRefreshProductUseInfo()
        {
            var product = CreateAvailableProduct("Cooling Product", UsageType.Cooling, 1);
            var useInfoObject = new GameObject("UseInfo");
            useInfoObject.transform.SetParent(product.transform);
            var useInfo = useInfoObject.AddComponent<UseInfoView>();
            var active = CreateImage(useInfoObject.transform, "Icon 01");
            var waiting = CreateImage(useInfoObject.transform, "Icon 02");
            active.color = Color.green;
            waiting.color = Color.gray;
            SetObjectReference(useInfo, "iconPoolParent", useInfoObject.transform);
            SetObjectReference(useInfo, "activeColorReference", active);
            SetObjectReference(useInfo, "waitingColorReference", waiting);

            var controllerObject = Track(new GameObject("Resident Demand Controller"));
            var controller = controllerObject.AddComponent<ResidentDemandController>();
            controller.InitializeForVerification(1);
            controller.RecomputeAssignmentsForVerification();
            var assignmentEvents = 0;
            controller.AssignmentsChanged += () => assignmentEvents++;

            var coordinatorObject = Track(new GameObject("Resident UI Coordinator"));
            coordinatorObject.SetActive(false);
            var coordinator = coordinatorObject.AddComponent<ResidentDemandUiCoordinator>();
            SetObjectReference(coordinator, "controller", controller);
            coordinatorObject.SetActive(true);
            coordinator.Subscribe();

            controller.AddResidentForVerification(CreateDemand());

            var productSnapshot = GetSnapshot(controller, product);
            Assert.That(productSnapshot.ActiveResidents.Count, Is.EqualTo(1));
            Assert.That(productSnapshot.WaitingResidents.Count, Is.Zero);
            Assert.That(useInfoObject.activeSelf, Is.True);
            Assert.That(active.gameObject.activeSelf, Is.True);
            Assert.That(waiting.gameObject.activeSelf, Is.False);

            product.SetPowered(false);

            Assert.That(assignmentEvents, Is.EqualTo(2));
            Assert.That(product.IsPowered, Is.False);
            var unpoweredSnapshot = GetSnapshot(controller, product);
            Assert.That(unpoweredSnapshot.ActiveResidents.Count, Is.Zero);
            Assert.That(useInfoObject.activeSelf, Is.False);
        }

        [Test]
        public void UseInfo_BindsOneOrderedAuthoredPoolWithActivePriorityAndOneWarning()
        {
            var product = CreateAvailableProduct("Product", UsageType.Cooling, 1);
            var useInfoObject = new GameObject("UseInfo");
            useInfoObject.transform.SetParent(product.transform);
            var view = useInfoObject.AddComponent<UseInfoView>();
            var activeReference = CreateImage(useInfoObject.transform, "Icon 01");
            var icon02 = CreateImage(useInfoObject.transform, "Icon 02");
            var waitingReference = CreateImage(useInfoObject.transform, "Icon 03");
            var icon04 = CreateImage(useInfoObject.transform, "Icon 04");
            var activeColor = new Color(1f, 0.4f, 0f, 1f);
            var waitingColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            activeReference.color = activeColor;
            waitingReference.color = waitingColor;
            SetObjectReference(view, "iconPoolParent", useInfoObject.transform);
            SetObjectReference(view, "activeColorReference", activeReference);
            SetObjectReference(view, "waitingColorReference", waitingReference);
            var childCount = useInfoObject.transform.childCount;

            view.Bind(CreateSnapshot(product, 2, 1));

            Assert.That(useInfoObject.activeSelf, Is.True);
            Assert.That(useInfoObject.transform.childCount, Is.EqualTo(childCount));
            Assert.That(activeReference.gameObject.activeSelf, Is.True);
            Assert.That(icon02.gameObject.activeSelf, Is.True);
            Assert.That(waitingReference.gameObject.activeSelf, Is.True);
            Assert.That(icon04.gameObject.activeSelf, Is.False);
            Assert.That(activeReference.color, Is.EqualTo(activeColor));
            Assert.That(icon02.color, Is.EqualTo(activeColor));
            Assert.That(waitingReference.color, Is.EqualTo(waitingColor));

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                "UseInfo authored icon pool has 4 slots.*requires 6.*active residents taking priority"));
            view.Bind(CreateSnapshot(product, 3, 3));
            view.Bind(CreateSnapshot(product, 4, 4));

            Assert.That(useInfoObject.transform.childCount, Is.EqualTo(childCount));
            Assert.That(activeReference.color, Is.EqualTo(activeColor));
            Assert.That(icon02.color, Is.EqualTo(activeColor));
            Assert.That(waitingReference.color, Is.EqualTo(activeColor));
            Assert.That(icon04.color, Is.EqualTo(activeColor));

            view.Bind(CreateSnapshot(product, 0, 0));
            Assert.That(useInfoObject.activeSelf, Is.False);
            Assert.That(CountActiveImages(useInfoObject.transform), Is.Zero);
        }

        [Test]
        public void UseInfo_UnpoweredProductIsHidden()
        {
            var productObject = Track(new GameObject("Product"));
            var product = productObject.AddComponent<ApplianceSource>();
            var useInfoObject = new GameObject("UseInfo");
            useInfoObject.transform.SetParent(productObject.transform);
            var view = useInfoObject.AddComponent<UseInfoView>();
            var active = CreateImage(useInfoObject.transform, "Icon 01");
            var waiting = CreateImage(useInfoObject.transform, "Icon 02");
            active.color = Color.green;
            waiting.color = Color.gray;
            SetObjectReference(view, "iconPoolParent", useInfoObject.transform);
            SetObjectReference(view, "activeColorReference", active);
            SetObjectReference(view, "waitingColorReference", waiting);

            view.Bind(CreateSnapshot(product, 1, 1));

            Assert.That(useInfoObject.activeSelf, Is.False);
        }

        private ResidentCardFixture CreateResidentCard(Sprite checkSprite = null)
        {
            return CreateResidentCard(null, "Resident Card", checkSprite);
        }

        private ResidentCardFixture CreateResidentCard(
            Transform parent,
            string name,
            Sprite checkSprite = null)
        {
            var root = Track(new GameObject(name));
            if (parent != null)
            {
                root.transform.SetParent(parent);
            }

            var view = root.AddComponent<ResidentCardView>();
            var numberObject = new GameObject("Number");
            numberObject.transform.SetParent(root.transform);
            var number = numberObject.AddComponent<TextMeshProUGUI>();
            var icon01 = CreateImage(root.transform, "Icon01");
            var icon02 = CreateImage(root.transform, "Icon02");
            var none = new GameObject("None");
            none.transform.SetParent(root.transform);
            var statusInfo = new GameObject("Status Info");
            statusInfo.transform.SetParent(root.transform);
            var slider = statusInfo.AddComponent<Slider>();
            var fill = CreateImage(statusInfo.transform, "Fill");
            var authoredGreen = new Color(0.07f, 0.42f, 0.37f, 1f);
            fill.color = authoredGreen;

            SetObjectReference(view, "residentNumber", number);
            SetObjectReference(view, "icon01", icon01);
            SetObjectReference(view, "icon02", icon02);
            SetObjectReference(view, "none", none);
            SetObjectReference(view, "completedCheckSprite", checkSprite);
            SetObjectReference(view, "statusInfo", statusInfo);
            SetObjectReference(view, "progressSlider", slider);
            SetObjectReference(view, "progressFill", fill);
            return new ResidentCardFixture(
                view,
                number,
                icon01,
                icon02,
                none,
                statusInfo,
                slider,
                fill,
                authoredGreen);
        }

        private UsageTypeIconLibrary CreateIconLibrary(
            Sprite coolingSprite,
            Sprite heatingSprite = null)
        {
            var library = Track(ScriptableObject.CreateInstance<UsageTypeIconLibrary>());
            var serialized = new SerializedObject(library);
            var entries = serialized.FindProperty("entries");
            entries.arraySize = heatingSprite == null ? 1 : 2;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("usageType").intValue =
                (int)UsageType.Cooling;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("icon").objectReferenceValue =
                coolingSprite;
            if (heatingSprite != null)
            {
                entries.GetArrayElementAtIndex(1).FindPropertyRelative("usageType").intValue =
                    (int)UsageType.Heating;
                entries.GetArrayElementAtIndex(1).FindPropertyRelative("icon").objectReferenceValue =
                    heatingSprite;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return library;
        }

        private Sprite CreateSprite()
        {
            var texture = Track(new Texture2D(1, 1));
            return Track(Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero));
        }

        private static DemandBalanceRecord CreateDemand(
            ResidentNeedType firstNeed = ResidentNeedType.Cooling,
            ResidentNeedType? secondNeed = null,
            float cooldownSeconds = 0f)
        {
            var needs = secondNeed.HasValue
                ? new[] { firstNeed, secondNeed.Value }
                : new[] { firstNeed };
            return new DemandBalanceRecord(
                "TEST-COLD",
                true,
                1,
                1,
                needs,
                10f,
                10f,
                0,
                0,
                0,
                cooldownSeconds);
        }

        private ApplianceSource CreateAvailableProduct(
            string name,
            UsageType usageType,
            int capacity)
        {
            var productObject = Track(new GameObject(name));
            var product = productObject.AddComponent<ApplianceSource>();
            var cableObject = new GameObject("Cable");
            cableObject.transform.SetParent(productObject.transform);
            var cable = cableObject.AddComponent<CableInfo>();
            var plugObject = new GameObject("Plug");
            plugObject.transform.SetParent(cableObject.transform);
            var plug = plugObject.AddComponent<PlugConnector>();
            var socketObject = Track(new GameObject($"{name} Socket"));
            var socket = socketObject.AddComponent<SocketConnector>();
            SetObjectReference(cable, "plug", plug);

            var serialized = new SerializedObject(product);
            serialized.FindProperty("cable").objectReferenceValue = cable;
            serialized.FindProperty("usageTypes").intValue = (int)usageType;
            serialized.FindProperty("capacity").intValue = capacity;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(PlugSocketConnection.Connect(plug, socket), Is.True);
            product.SetPowered(true);
            return product;
        }

        private static ProductUsageSnapshot GetSnapshot(
            ResidentDemandController controller,
            ApplianceSource product)
        {
            foreach (var snapshot in controller.ProductUsage)
            {
                if (snapshot.Product == product)
                {
                    return snapshot;
                }
            }

            Assert.Fail($"No Product usage snapshot was found for '{product.name}'.");
            return null;
        }

        private static ProductUsageSnapshot CreateSnapshot(
            ApplianceSource product,
            int active,
            int waiting)
        {
            var activeResidents = new List<ResidentNumber>();
            var waitingResidents = new List<ResidentNumber>();
            for (var index = 0; index < active; index++)
            {
                activeResidents.Add(new ResidentNumber(index + 1));
            }

            for (var index = 0; index < waiting; index++)
            {
                waitingResidents.Add(new ResidentNumber(active + index + 1));
            }

            return new ProductUsageSnapshot(
                "product",
                product,
                activeResidents,
                waitingResidents);
        }

        private static Image CreateImage(Transform parent, string name)
        {
            var imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent);
            return imageObject.AddComponent<Image>();
        }

        private static int CountActiveImages(Transform parent)
        {
            var count = 0;
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeSelf && child.GetComponent<Image>() != null)
                {
                    count++;
                }
            }

            return count;
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

        private T Track<T>(T target)
            where T : Object
        {
            _objects.Add(target);
            return target;
        }

        private sealed class ResidentCardFixture
        {
            public ResidentCardFixture(
                ResidentCardView view,
                TMP_Text number,
                Image icon01,
                Image icon02,
                GameObject none,
                GameObject statusInfo,
                Slider slider,
                Image fill,
                Color authoredGreen)
            {
                View = view;
                Number = number;
                Icon01 = icon01;
                Icon02 = icon02;
                None = none;
                StatusInfo = statusInfo;
                Slider = slider;
                Fill = fill;
                AuthoredGreen = authoredGreen;
            }

            public ResidentCardView View { get; }
            public TMP_Text Number { get; }
            public Image Icon01 { get; }
            public Image Icon02 { get; }
            public GameObject None { get; }
            public GameObject StatusInfo { get; }
            public Slider Slider { get; }
            public Image Fill { get; }
            public Color AuthoredGreen { get; }
        }
    }
}
