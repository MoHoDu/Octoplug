using System.IO;
using NUnit.Framework;
using Octoplug.Telemetry;
using UnityEngine;

namespace Octoplug.Tests.Editor.Telemetry
{
    public sealed class SessionTelemetryRecorderTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Application.temporaryCachePath, "OctoplugTelemetryTests", System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            SessionTelemetryService.SetRecorderForVerification(null);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void Recorder_AppendsChronologicalEvents_AndFinalizesOneArtifact()
        {
            var time = 0f;
            var recorder = new SessionTelemetryRecorder(root, () => time);
            recorder.Start();
            time = 2f;
            recorder.Append("ObjectSpawned", "Object", "Product", "product-1", "room-1", "{}");
            time = 5f;

            Assert.That(recorder.TryFinalize("Test", out var artifact), Is.True);
            Assert.That(File.Exists(artifact.Path), Is.True);
            Assert.That(recorder.Document.IsFinalized, Is.True);
            Assert.That(recorder.Document.Events, Has.Count.EqualTo(3));
            Assert.That(recorder.Document.Events[0].EventSeq, Is.EqualTo(1));
            Assert.That(recorder.Document.Events[1].EventSeq, Is.EqualTo(2));
            Assert.That(recorder.Document.Events[2].EventSeq, Is.EqualTo(3));
            Assert.That(recorder.Document.Events[1].EventTimeSec, Is.EqualTo(2f));
            Assert.That(recorder.Document.Summary.DurationSec, Is.EqualTo(5f));

            var json = File.ReadAllText(artifact.Path);
            Assert.That(json, Does.Contain("\"LogSchemaVersion\": 1"));
            Assert.That(json, Does.Contain("\"IsFinalized\": true"));
            Assert.That(json, Does.Contain("ObjectSpawned"));
        }

        [Test]
        public void UserIdPersists_ButSessionIdChanges()
        {
            var first = new SessionTelemetryRecorder(root, () => 0f);
            first.Start();
            first.TryFinalize("Test", out _);
            var second = new SessionTelemetryRecorder(root, () => 0f);
            second.Start();

            Assert.That(second.Document.UserID, Is.EqualTo(first.Document.UserID));
            Assert.That(second.Document.SessionID, Is.Not.EqualTo(first.Document.SessionID));
        }

        [Test]
        public void GenerateInspectableArtifact()
        {
            var outputRoot = Path.Combine(
                Application.temporaryCachePath,
                "OctoplugTelemetryInspectableSample");
            var recorder = new SessionTelemetryRecorder(outputRoot, () => 12.5f);
            recorder.Start();
            recorder.Append("RoomUnlocked", "World", "Room", "room-sample", "room-sample", "{\"MinX\":0,\"MinY\":0,\"MaxX\":10,\"MaxY\":8}");
            recorder.Append("GlobalSnapshot", "Snapshot", "Session", recorder.Document.SessionID, string.Empty, "{\"Experience\":3,\"Satisfaction\":92,\"AllowedPowerWatts\":22}");

            Assert.That(recorder.TryFinalize("AutomatedSample", out var artifact), Is.True);
            Assert.That(File.Exists(artifact.Path), Is.True);
            TestContext.Progress.WriteLine(artifact.Path);
        }

        [Test]
        public void LatestCompleted_IgnoresCheckpoint()
        {
            var recorder = new SessionTelemetryRecorder(root, () => 0f);
            recorder.Start();
            Assert.That(recorder.TryCheckpoint(out var checkpoint), Is.True);
            Assert.That(File.Exists(checkpoint), Is.True);
            Assert.That(recorder.TryGetLatestCompletedSession(out _), Is.False);

            Assert.That(recorder.TryFinalize("Test", out var expected), Is.True);
            Assert.That(recorder.TryGetLatestCompletedSession(out var actual), Is.True);
            Assert.That(actual.Path, Is.EqualTo(expected.Path));
        }

        [Test]
        public void RuntimeDisable_FinalizesActiveSessionExactlyOnce()
        {
            var recorder = new SessionTelemetryRecorder(root, () => 3f);
            recorder.Start();
            SessionTelemetryService.SetRecorderForVerification(recorder);
            var owner = new GameObject("Session Telemetry Test");
            var runtime = owner.AddComponent<SessionTelemetryRuntime>();

            runtime.FinalizeForVerification("RuntimeDisabled");

            Assert.That(recorder.Document.IsFinalized, Is.True);
            Assert.That(recorder.TryGetLatestCompletedSession(out var artifact), Is.True);
            Assert.That(File.Exists(artifact.Path), Is.True);
            Assert.That(recorder.Document.Events.FindAll(value => value.EventType == "SessionEnded"), Has.Count.EqualTo(1));

            Object.DestroyImmediate(owner);

            Assert.That(recorder.Document.Events.FindAll(value => value.EventType == "SessionEnded"), Has.Count.EqualTo(1));
            Assert.That(Directory.GetFiles(Path.GetDirectoryName(artifact.Path), "*_completed.json"), Has.Length.EqualTo(1));
        }
    }
}
