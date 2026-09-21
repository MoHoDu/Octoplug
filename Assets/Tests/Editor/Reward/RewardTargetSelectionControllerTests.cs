using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Power;
using Octoplug.Reward;
using Octoplug.Reward.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Octoplug.Tests.Reward.Unity
{
    public class RewardTargetSelectionControllerTests
    {
        private GameObject root;
        private RewardTargetSelectionController selectionController;
        private PowerStrip strip;
        private Collider2D stripCollider;

        [SetUp] public void Setup()
        {
            

            root = new GameObject("TestRoot");
            selectionController = root.AddComponent<RewardTargetSelectionController>();

            // Setup a mock PowerStrip
            var stripObj = new GameObject("Strip");
            stripObj.transform.SetParent(root.transform);
            strip = stripObj.AddComponent<PowerStrip>();

            // Add fake sockets so AreEffectsApplicable can check it
            var socketsField = typeof(PowerStrip).GetField("sockets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sockets = new List<SocketConnector>();
            for(int i = 0; i < 2; i++) sockets.Add(new GameObject("Socket").AddComponent<SocketConnector>());
            socketsField.SetValue(strip, sockets);

            stripCollider = stripObj.AddComponent<BoxCollider2D>();
            var sr = stripObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
        }

        [TearDown] public void TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            
        }

        private class MockSelectionContext : IRewardContext
        {
            private List<PowerStrip> strips = new();
            public void AddStrip(PowerStrip s) => strips.Add(s);

            public int CurrentRoomCount => 1;
            public float CurrentHouseAllowedPower => 10f;
            public float MaxHouseAllowedPower => 20f;
            public IEnumerable<PowerStrip> GetPowerStrips() => strips;
            public IEnumerable<CableOwnerRewardTarget> GetCableOwners() =>
                Array.Empty<CableOwnerRewardTarget>();
            public IEnumerable<RoomPlacementRewardTarget> GetRoomPlacements() =>
                Array.Empty<RoomPlacementRewardTarget>();
        }

        [Test] public void TargetSelection_HighlightsValidStrip_AndWaitsForClick()
        {
            var context = new MockSelectionContext();
            context.AddStrip(strip);

            // Cable length has a non-mutating applicability check and requires no grid reservation.
            var cableObject = new GameObject("Cable");
            cableObject.transform.SetParent(strip.transform);
            var cable = cableObject.AddComponent<CableInfo>();
            typeof(PowerStrip).GetField("cable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(strip, cable);
            var reward = new RewardBalanceRecord("TEST_REWARD", true, 1, 1, "name", "description", RewardTargetType.PowerStrip,
                new[] { new RewardEffect(RewardEffectType.PowerStripAllowedPower, 1) });

            object selectedTarget = null;
            selectionController.BeginSelection(reward, context, target =>
            {
                selectedTarget = target;
                return true;
            });

            // Overlay is camera-owned in production so it remains screen-covering while panning.
            var overlay = UnityEngine.Object.FindFirstObjectByType<SpriteRenderer>();
            Assert.That(
                overlay != null && overlay.gameObject.name == "RewardTargetSelectionOverlay",
                Is.True,
                "Overlay should be created");

            // Strip's sorting order should be bumped
            var sr = strip.GetComponent<SpriteRenderer>();
            Assert.That(sr.sortingOrder, Is.GreaterThan(1000), "Eligible strip should have sorting order shifted above overlay");

            // Call ResolveClick manually via reflection
            var resolveMethod = typeof(RewardTargetSelectionController).GetMethod("ResolveClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var missResult = resolveMethod.Invoke(selectionController, new object[] { new Vector2(100f, 100f) });
            Assert.That(missResult, Is.Null, "Clicking outside should return null");
            
            var hitResult = resolveMethod.Invoke(selectionController, new object[] { Vector2.zero });
            Assert.That(hitResult, Is.EqualTo(strip), "Clicking on the strip should return it");
            
            // Manually complete
            var completeMethod = typeof(RewardTargetSelectionController).GetMethod("CompleteSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            completeMethod.Invoke(selectionController, new object[] { strip });
            
            Assert.That(selectedTarget, Is.EqualTo(strip), "Callback should be invoked with target");
            
            Assert.That(
                UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None),
                Has.None.Matches<SpriteRenderer>(renderer => renderer.gameObject.name == "RewardTargetSelectionOverlay"),
                "Overlay should be destroyed after selection");
            Assert.That(sr.sortingOrder, Is.EqualTo(5), "Sorting order should be restored");
        }
    }
}






