using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Octoplug.SurveySubmission;
using Octoplug.Telemetry;
using UnityEngine;

namespace Octoplug.Tests.Editor.SurveySubmission
{
    public sealed class SurveySubmissionTests
    {
        private string root;
        private string previousToken;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Application.temporaryCachePath, "OctoplugSurveyTests", Guid.NewGuid().ToString("N"));
            previousToken = Environment.GetEnvironmentVariable(SurveySubmissionConfig.UploadTokenEnvironmentVariable);
            Environment.SetEnvironmentVariable(SurveySubmissionConfig.UploadTokenEnvironmentVariable, "test-token");
        }

        [TearDown]
        public void TearDown()
        {
            SessionTelemetryService.SetRecorderForVerification(null);
            Environment.SetEnvironmentVariable(SurveySubmissionConfig.UploadTokenEnvironmentVariable, previousToken);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        [Test]
        public void Config_UsesExactProductionEndpointAndFormEntries()
        {
            Assert.That(SurveySubmissionConfig.UploadEndpoint, Does.EndWith("/exec"));
            Assert.That(SurveySubmissionConfig.UploadEndpoint, Does.Not.Contain("/dev"));
            Assert.That(SurveySubmissionConfig.UserIdEntry, Is.EqualTo("entry.1392202199"));
            Assert.That(SurveySubmissionConfig.SessionIdEntry, Is.EqualTo("entry.480603338"));
            Assert.That(SurveySubmissionConfig.LogFileUrlEntry, Is.EqualTo("entry.1489638143"));
            Assert.That(SurveySubmissionConfig.LogSchemaVersionEntry, Is.EqualTo("entry.564784767"));
        }

        [Test]
        public void Payload_MapsExactFieldsAndEmbedsFullFinalizedJsonObject()
        {
            var request = new SurveyUploadRequest(
                "secret",
                "user-1",
                "session-1",
                1,
                "session.json",
                "{\"IsFinalized\":true,\"Events\":[{\"EventType\":\"GameOver\"}]}");

            Assert.That(SurveyUploadPayloadBuilder.TryBuild(request, out var payload, out _), Is.True);
            Assert.That(payload, Does.Contain("\"upload_token\":\"secret\""));
            Assert.That(payload, Does.Contain("\"user_id\":\"user-1\""));
            Assert.That(payload, Does.Contain("\"session_id\":\"session-1\""));
            Assert.That(payload, Does.Contain("\"schema_version\":1"));
            Assert.That(payload, Does.Contain("\"file_name\":\"session.json\""));
            Assert.That(payload, Does.Contain("\"telemetry_json\":{\"IsFinalized\":true"));
            Assert.That(payload, Does.Not.Contain("\"telemetry_json\":\""));
        }

        [Test]
        public void Payload_RejectsCheckpoint()
        {
            var request = new SurveyUploadRequest("secret", "user", "session", 1, "checkpoint.json", "{\"IsFinalized\":false}");
            Assert.That(SurveyUploadPayloadBuilder.TryBuild(request, out _, out _), Is.False);
        }

        [Test]
        public void ResponseParser_AcceptsSuccessAndAlreadyExists()
        {
            var uploaded = SurveyUploadResponseParser.Parse(
                "{\"success\":true,\"already_exists\":false,\"file_id\":\"new\",\"file_url\":\"https://drive.google.com/new\"}");
            var existing = SurveyUploadResponseParser.Parse(
                "{\"success\":true,\"already_exists\":true,\"file_id\":\"old\",\"file_url\":\"https://drive.google.com/old\"}");

            Assert.That(uploaded.Succeeded, Is.True);
            Assert.That(uploaded.AlreadyExists, Is.False);
            Assert.That(existing.Succeeded, Is.True);
            Assert.That(existing.AlreadyExists, Is.True);
        }

        [TestCase("")]
        [TestCase("not-json")]
        [TestCase("{\"success\":false}")]
        [TestCase("{\"success\":true,\"file_url\":\"http://drive.google.com/file\"}")]
        [TestCase("{\"success\":true,\"file_url\":\"not-a-url\"}")]
        public void ResponseParser_RejectsInvalidOrFailedResponses(string response)
        {
            Assert.That(SurveyUploadResponseParser.Parse(response).Succeeded, Is.False);
        }

        [Test]
        public void UrlBuilder_EncodesFileUrlAndAllExactValues()
        {
            var url = SurveyUrlBuilder.Build("user 1", "session&1", "https://drive.google.com/file/d/a?x=1&y=2", 1);

            Assert.That(url, Does.Contain("entry.1392202199=user%201"));
            Assert.That(url, Does.Contain("entry.480603338=session%261"));
            Assert.That(url, Does.Contain("entry.1489638143=https%3A%2F%2Fdrive.google.com%2Ffile%2Fd%2Fa%3Fx%3D1%26y%3D2"));
            Assert.That(url, Does.Contain("entry.564784767=1"));
        }

        [Test]
        public void MetadataStore_PersistsUploadedFileUrlAndRecoversStaleUploading()
        {
            var store = new SurveyUploadMetadataStore(root);
            var metadata = store.Load("session-1");
            metadata.UploadStatus = SurveyUploadStatus.Uploaded;
            metadata.UploadedFileID = "file-1";
            metadata.UploadedFileURL = "https://drive.google.com/file-1";
            Assert.That(store.Save(metadata), Is.True);

            var loaded = store.Load("session-1");
            Assert.That(loaded.UploadStatus, Is.EqualTo(SurveyUploadStatus.Uploaded));
            Assert.That(loaded.UploadedFileURL, Is.EqualTo("https://drive.google.com/file-1"));

            loaded.UploadStatus = SurveyUploadStatus.Uploading;
            store.Save(loaded);
            Assert.That(store.Load("session-1").UploadStatus, Is.EqualTo(SurveyUploadStatus.Failed));
        }

        [Test]
        public void Coordinator_UploadsOnceThenReusesPersistedUrl()
        {
            var recorder = CreateFinalizedRecorder();
            SessionTelemetryService.SetRecorderForVerification(recorder);
            var transport = new FakeTransport(SurveyUploadResult.Success(false, "file-1", "https://drive.google.com/file-1"));
            var opener = new FakeOpener();
            var coordinator = new SurveySubmissionCoordinator(transport, opener, new SurveyUploadMetadataStore(root));

            Run(coordinator.SubmitLatest());
            Run(coordinator.SubmitLatest());

            Assert.That(transport.CallCount, Is.EqualTo(1));
            Assert.That(opener.Urls, Has.Count.EqualTo(2));
            Assert.That(opener.Urls[0], Is.EqualTo(opener.Urls[1]));
        }

        [Test]
        public void Coordinator_FailedUploadCanRetry()
        {
            var recorder = CreateFinalizedRecorder();
            SessionTelemetryService.SetRecorderForVerification(recorder);
            var transport = new FakeTransport(
                SurveyUploadResult.Failure("network"),
                SurveyUploadResult.Success(true, "file-1", "https://drive.google.com/file-1"));
            var opener = new FakeOpener();
            var coordinator = new SurveySubmissionCoordinator(transport, opener, new SurveyUploadMetadataStore(root));
            var outcomes = new List<SurveySubmissionOutcome>();

            Run(coordinator.SubmitLatest(outcomes.Add));
            Run(coordinator.SubmitLatest(outcomes.Add));

            Assert.That(outcomes, Is.EqualTo(new[] { SurveySubmissionOutcome.UploadFailed, SurveySubmissionOutcome.Opened }));
            Assert.That(transport.CallCount, Is.EqualTo(2));
            Assert.That(opener.Urls, Has.Count.EqualTo(1));
        }

        [Test]
        public void Coordinator_StopsBeforeUploadWhenUploadingStateCannotBePersisted()
        {
            var recorder = CreateFinalizedRecorder();
            SessionTelemetryService.SetRecorderForVerification(recorder);
            var transport = new FakeTransport(SurveyUploadResult.Success(false, "file", "https://drive.google.com/file"));
            var opener = new FakeOpener();
            var outcome = SurveySubmissionOutcome.Opened;

            Run(new SurveySubmissionCoordinator(transport, opener, new FailingMetadataStore(root))
                .SubmitLatest(value => outcome = value));

            Assert.That(outcome, Is.EqualTo(SurveySubmissionOutcome.PersistenceFailed));
            Assert.That(transport.CallCount, Is.Zero);
            Assert.That(opener.Urls, Is.Empty);
        }

        [Test]
        public void Coordinator_NoCompletedSessionDoesNotUploadOrOpen()
        {
            var recorder = new SessionTelemetryRecorder(root);
            recorder.Start();
            recorder.TryCheckpoint(out _);
            SessionTelemetryService.SetRecorderForVerification(recorder);
            var transport = new FakeTransport(SurveyUploadResult.Success(false, "file", "https://drive.google.com/file"));
            var opener = new FakeOpener();
            var outcome = SurveySubmissionOutcome.Opened;

            Run(new SurveySubmissionCoordinator(transport, opener, new SurveyUploadMetadataStore(root))
                .SubmitLatest(value => outcome = value));

            Assert.That(outcome, Is.EqualTo(SurveySubmissionOutcome.NoCompletedSession));
            Assert.That(transport.CallCount, Is.Zero);
            Assert.That(opener.Urls, Is.Empty);
        }

        private SessionTelemetryRecorder CreateFinalizedRecorder()
        {
            var recorder = new SessionTelemetryRecorder(root);
            recorder.Start();
            Assert.That(recorder.TryFinalize("Test", out _), Is.True);
            return recorder;
        }

        private static void Run(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                var current = stack.Peek();
                if (!current.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                if (current.Current is IEnumerator nested) stack.Push(nested);
            }
        }

        private sealed class FakeTransport : ISurveyUploadTransport
        {
            private readonly Queue<SurveyUploadResult> results;

            public FakeTransport(params SurveyUploadResult[] results)
            {
                this.results = new Queue<SurveyUploadResult>(results);
            }

            public int CallCount { get; private set; }

            public IEnumerator Upload(SurveyUploadRequest request, Action<SurveyUploadResult> completed)
            {
                CallCount++;
                completed(results.Dequeue());
                yield break;
            }
        }

        private sealed class FakeOpener : ISurveyUrlOpener
        {
            public List<string> Urls { get; } = new();
            public void Open(string url) => Urls.Add(url);
        }

        private sealed class FailingMetadataStore : SurveyUploadMetadataStore
        {
            public FailingMetadataStore(string persistentDataPath) : base(persistentDataPath)
            {
            }

            public override bool Save(SurveyUploadMetadata metadata) => false;
        }
    }
}
