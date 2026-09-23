using System;
using System.Collections;
using System.IO;
using Octoplug.Telemetry;
using UnityEngine;

namespace Octoplug.SurveySubmission
{
    public sealed class SurveySubmissionCoordinator
    {
        private readonly ISurveyUploadTransport transport;
        private readonly ISurveyUrlOpener opener;
        private readonly SurveyUploadMetadataStore metadataStore;
        private bool isRunning;
        private SurveySubmissionOutcome lastOutcome;

        public SurveySubmissionCoordinator(ISurveyUploadTransport transport, ISurveyUrlOpener opener, SurveyUploadMetadataStore metadataStore)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.opener = opener ?? throw new ArgumentNullException(nameof(opener));
            this.metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
        }

        public IEnumerator SubmitLatest(Action<SurveySubmissionOutcome> completed = null)
        {
            if (isRunning)
            {
                while (isRunning) yield return null;
                completed?.Invoke(lastOutcome);
                yield break;
            }

            isRunning = true;
            var outcome = SurveySubmissionOutcome.UploadFailed;
            try
            {
                if (!SessionTelemetryService.TryGetLatestCompletedSession(out var artifact))
                {
                    outcome = SurveySubmissionOutcome.NoCompletedSession;
                    yield break;
                }

                var metadata = metadataStore.Load(artifact.SessionID);
                string fileUrl;
                if (metadata.UploadStatus == SurveyUploadStatus.Uploaded && IsHttps(metadata.UploadedFileURL))
                {
                    fileUrl = metadata.UploadedFileURL;
                }
                else
                {
                    string telemetryJson;
                    try
                    {
                        telemetryJson = File.ReadAllText(artifact.Path);
                    }
                    catch
                    {
                        outcome = SurveySubmissionOutcome.InvalidArtifact;
                        yield break;
                    }

                    if (!RuntimeUploadTokenProvider.TryGet(out var token))
                    {
                        outcome = SurveySubmissionOutcome.MissingToken;
                        yield break;
                    }

                    metadata.UploadStatus = SurveyUploadStatus.Uploading;
                    metadata.LastError = string.Empty;
                    if (!metadataStore.Save(metadata))
                    {
                        outcome = SurveySubmissionOutcome.PersistenceFailed;
                        yield break;
                    }

                    var request = new SurveyUploadRequest(
                        token,
                        artifact.UserID,
                        artifact.SessionID,
                        artifact.LogSchemaVersion,
                        Path.GetFileName(artifact.Path),
                        telemetryJson);
                    var uploadResult = SurveyUploadResult.Failure("Upload did not complete.");
                    yield return transport.Upload(request, value => uploadResult = value);
                    token = string.Empty;

                    if (!uploadResult.Succeeded)
                    {
                        metadata.UploadStatus = SurveyUploadStatus.Failed;
                        metadata.LastError = uploadResult.Error;
                        outcome = metadataStore.Save(metadata)
                            ? SurveySubmissionOutcome.UploadFailed
                            : SurveySubmissionOutcome.PersistenceFailed;
                        yield break;
                    }

                    metadata.UploadStatus = SurveyUploadStatus.Uploaded;
                    metadata.UploadedFileID = uploadResult.FileID;
                    metadata.UploadedFileURL = uploadResult.FileURL;
                    metadata.UploadedAtUtc = DateTime.UtcNow.ToString("O");
                    metadata.LastError = string.Empty;
                    if (!metadataStore.Save(metadata))
                    {
                        outcome = SurveySubmissionOutcome.PersistenceFailed;
                        yield break;
                    }
                    fileUrl = uploadResult.FileURL;
                }

                try
                {
                    opener.Open(SurveyUrlBuilder.Build(artifact.UserID, artifact.SessionID, fileUrl, artifact.LogSchemaVersion));
                    outcome = SurveySubmissionOutcome.Opened;
                }
                catch
                {
                    outcome = SurveySubmissionOutcome.OpenFailed;
                }
            }
            finally
            {
                lastOutcome = outcome;
                isRunning = false;
                completed?.Invoke(outcome);
            }
        }

        private static bool IsHttps(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    public static class SurveySubmissionRuntime
    {
        private const int FinalizationWaitFrames = 10;
        private static Host host;

        public static SurveySubmissionCoordinator CreateDefault() => new(
            new UnityWebRequestSurveyUploadTransport(),
            new ApplicationSurveyUrlOpener(),
            new SurveyUploadMetadataStore(Application.persistentDataPath));

        public static void SubmitLatest(Action<SurveySubmissionOutcome> completed, bool waitForFinalization = false)
        {
            EnsureHost().Submit(completed, waitForFinalization);
        }

        private static Host EnsureHost()
        {
            if (host != null) return host;
            var gameObject = new GameObject("Survey Submission Runtime");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            host = gameObject.AddComponent<Host>();
            return host;
        }

        private sealed class Host : MonoBehaviour
        {
            private SurveySubmissionCoordinator coordinator;

            private void Awake()
            {
                coordinator = CreateDefault();
            }

            public void Submit(Action<SurveySubmissionOutcome> completed, bool waitForFinalization)
            {
                StartCoroutine(SubmitRoutine(completed, waitForFinalization));
            }

            private IEnumerator SubmitRoutine(Action<SurveySubmissionOutcome> completed, bool waitForFinalization)
            {
                var attemptsRemaining = waitForFinalization ? FinalizationWaitFrames + 1 : 1;
                while (attemptsRemaining-- > 0)
                {
                    var outcome = SurveySubmissionOutcome.UploadFailed;
                    yield return coordinator.SubmitLatest(value => outcome = value);
                    if (outcome != SurveySubmissionOutcome.NoCompletedSession || attemptsRemaining == 0)
                    {
                        completed?.Invoke(outcome);
                        yield break;
                    }

                    yield return null;
                }
            }
        }
    }
}
