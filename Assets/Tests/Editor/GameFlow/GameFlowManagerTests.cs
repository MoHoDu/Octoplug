using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.CameraFraming.Unity;
using Octoplug.GameFlow;
using Octoplug.GameFlow.Unity;
using Octoplug.Power;
using Octoplug.Power.Grid;
using Octoplug.ResidentDemand;
using Octoplug.ResidentDemand.Unity;
using Octoplug.RoomGeneration.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Octoplug.Tests.Editor.GameFlow
{
    public sealed class GameFlowManagerTests
    {
        private const string RoomPrefabPath = "Assets/03_Prefabs/Rooms/Room.prefab";

        private readonly List<GameObject> createdObjects = new();
        private GameFlowManager gameFlow;
        private SessionProgressController sessionProgress;
        private ProductionRoomGenerationController roomGen;
        private ResidentDemandController residentDemand;
        private RoomGenerationCameraFramingBridge cameraBridge;

        [SetUp]
        public void SetUp()
        {
            var roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            Assert.That(roomPrefab, Is.Not.Null);

            var root = new GameObject("Root");
            createdObjects.Add(root);

            // Room Generation Setup
            var roomsRoot = new GameObject("Rooms").transform;
            roomsRoot.SetParent(root.transform);

            var seedObject = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab, roomsRoot);
            var seedBinder = seedObject.GetComponent<RoomGenerationRoomBinder>();
            var prefabBinder = roomPrefab.GetComponent<RoomGenerationRoomBinder>();

            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(root.transform);
            var gridService = gridObject.AddComponent<CableRoutingGridService>();
            gridService.InitializeForVerification();

            var roomGenObj = new GameObject("RoomGen");
            roomGenObj.transform.SetParent(root.transform);
            roomGen = roomGenObj.AddComponent<ProductionRoomGenerationController>();

            var so = new SerializedObject(roomGen);
            so.FindProperty("seedRoom").objectReferenceValue = seedBinder;
            so.FindProperty("roomPrefab").objectReferenceValue = prefabBinder;
            so.FindProperty("roomsRoot").objectReferenceValue = roomsRoot;
            so.FindProperty("routingGrid").objectReferenceValue = gridService;
            so.FindProperty("seedRoomId").stringValue = "room-seed";
            so.ApplyModifiedProperties();

            roomGen.Initialize();
            Assert.That(roomGen.State.HasNextRoomPlan, Is.True, "RoomGen should have next room plan after Initialize");

            // Resident Demand Setup
            var residentDemandObj = new GameObject("ResidentDemand");
            residentDemandObj.transform.SetParent(root.transform);
            residentDemand = residentDemandObj.AddComponent<ResidentDemandController>();

            var rdSo = new SerializedObject(residentDemand);
            rdSo.FindProperty("roomGeneration").objectReferenceValue = roomGen;
            rdSo.FindProperty("initialResidentCount").intValue = 0;
            rdSo.ApplyModifiedProperties();

            residentDemand.InitializeForVerification(1);

            // Session Progress Setup
            var sessionObj = new GameObject("SessionProgress");
            sessionObj.transform.SetParent(root.transform);
            sessionProgress = sessionObj.AddComponent<SessionProgressController>();

            var table = DefaultRequiredExperience.Create();
            sessionProgress.InitializeForVerification(residentDemand, 50, 0, 1, table);

            // Camera Bridge Setup
            var bridgeObj = new GameObject("CameraBridge");
            bridgeObj.transform.SetParent(root.transform);
            cameraBridge = bridgeObj.AddComponent<RoomGenerationCameraFramingBridge>();

            // GameFlow Setup
            var gfObj = new GameObject("GameFlow");
            gfObj.transform.SetParent(root.transform);
            gameFlow = gfObj.AddComponent<GameFlowManager>();
            var gfSo = new SerializedObject(gameFlow);
            gfSo.FindProperty("sessionProgress").objectReferenceValue = sessionProgress;
            gfSo.FindProperty("roomGeneration").objectReferenceValue = roomGen;
            gfSo.FindProperty("residentDemand").objectReferenceValue = residentDemand;
            gfSo.FindProperty("cameraBridge").objectReferenceValue = cameraBridge;
            gfSo.ApplyModifiedProperties();

            // Force OnEnable via verification method
            gameFlow.InitializeForVerification();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (var obj in createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            createdObjects.Clear();
        }

        private DemandOutcome CreateDummyOutcome(DemandResolution resolution, int exp, int satFail)
        {
            var demand = new DemandBalanceRecord(
                "dummy", true, 1, 1, new[] { ResidentNeedType.Cooling },
                1f, 1f, exp, 0, satFail, 1f);
            return new DemandOutcome(demand, resolution);
        }

        [UnityTest]
        public IEnumerator ScenariosAtoH_FullFlow()
        {
            // Initial State
            Assert.That(gameFlow.CurrentState, Is.EqualTo(GameFlowState.Playing));
            var initialResidents = residentDemand.Residents.Count;
            Assert.That(roomGen.State.UnlockedLayout.Rooms.Count, Is.EqualTo(1));

            // A. EXP Threshold
            bool rewardRequestedFired = false;
            gameFlow.RewardPhaseRequested += () => rewardRequestedFired = true;

            // Trigger EXP max (requires 20)
            sessionProgress.ApplyOutcomeForVerification(CreateDummyOutcome(DemandResolution.Success, 20, 0));

            // B. Room: Promote happens immediately
            // C. Resident: +1 Resident happens immediately
            // State is CameraReveal immediately because promotion is synchronous
            Assert.That(gameFlow.CurrentState, Is.EqualTo(GameFlowState.CameraReveal));
            Assert.That(roomGen.State.UnlockedLayout.Rooms.Count, Is.EqualTo(2));
            Assert.That(residentDemand.Residents.Count, Is.EqualTo(initialResidents + 1));

            // Simulate second threshold while in progression to ensure no duplicate progression
            sessionProgress.ApplyOutcomeForVerification(CreateDummyOutcome(DemandResolution.Success, 200, 0));
            Assert.That(roomGen.State.UnlockedLayout.Rooms.Count, Is.EqualTo(2), "Should not promote again while in progression");

            // D. Camera Reveal completion
            var method = typeof(RoomGenerationCameraFramingBridge).GetMethod("OnCameraRevealCompleted", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(cameraBridge, null);

            // E. Pause / Delay
            Assert.That(gameFlow.CurrentState, Is.EqualTo(GameFlowState.RewardDelay));
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            // Wait 1.1s in realtime using EditorApplication time because we're in EditMode
            double start = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - start < 1.1)
            {
                // Must step the coroutine manually in edit mode for WaitForSecondsRealtime
                // But since GameFlowManager uses StartCoroutine, it might not advance in EditMode.
                // We'll call a reflection hack to step the enumerator if needed, or we just trust the state transition if it runs.
                // Actually, StartCoroutine doesn't run in EditMode.
                yield return null;
            }

            // Since StartCoroutine doesn't advance in EditMode automatically without a runner,
            // we manually complete the delay for testing purposes if it hasn't advanced.
            if (gameFlow.CurrentState == GameFlowState.RewardDelay)
            {
                // Force it if coroutine didn't run (EditMode limitation)
                var rewardMethod = typeof(GameFlowManager).GetMethod("RewardDelayRoutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var enumerator = (IEnumerator)rewardMethod.Invoke(gameFlow, null);
                enumerator.MoveNext(); // Time.timeScale = 0, yields WaitForSecondsRealtime
                enumerator.MoveNext(); // finishes delay, sets state, invokes event
            }

            // F. Reward Boundary
            Assert.That(gameFlow.CurrentState, Is.EqualTo(GameFlowState.AwaitingReward));
            Assert.That(rewardRequestedFired, Is.True);

            gameFlow.CompleteRewardPhase();
            Assert.That(gameFlow.CurrentState, Is.EqualTo(GameFlowState.CameraReveal), "Queued EXP should trigger immediately.");
            Assert.That(roomGen.State.UnlockedLayout.Rooms.Count, Is.EqualTo(3), "Room 3 should be promoted");
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            // G. EXP Cycle Reset
            Assert.That(sessionProgress.RequiredExperience, Is.EqualTo(40), "Required EXP should update to next tier based on room count 2.");
            Assert.That(sessionProgress.CurrentExperience, Is.EqualTo(0), "EXP should be reset to 0 based on current policy.");

            // H. GameOver
            bool gameOverFired = false;
            gameFlow.GameOverRequested += () => gameOverFired = true;

            // Deplete satisfaction
            sessionProgress.ApplyOutcomeForVerification(CreateDummyOutcome(DemandResolution.Failure, 0, -100));
            Assert.That(gameFlow.CurrentState, Is.EqualTo(GameFlowState.GameOver));
            Assert.That(gameOverFired, Is.True);

            // Ensure no progression happens during game over
            sessionProgress.ApplyOutcomeForVerification(CreateDummyOutcome(DemandResolution.Success, 500, 0));
            Assert.That(roomGen.State.UnlockedLayout.Rooms.Count, Is.EqualTo(3));
        }
    }
}
