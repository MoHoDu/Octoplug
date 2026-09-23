using System;

namespace Octoplug.SurveySubmission
{
    public enum SurveyUploadStatus
    {
        NotUploaded,
        Uploading,
        Uploaded,
        Failed
    }

    [Serializable]
    public sealed class SurveyUploadMetadata
    {
        public string SessionID;
        public SurveyUploadStatus UploadStatus;
        public string UploadedFileID;
        public string UploadedFileURL;
        public string UploadedAtUtc;
        public string LastError;
    }

    public readonly struct SurveyUploadRequest
    {
        public SurveyUploadRequest(string token, string userId, string sessionId, int schemaVersion, string fileName, string telemetryJson)
        {
            Token = token;
            UserID = userId;
            SessionID = sessionId;
            SchemaVersion = schemaVersion;
            FileName = fileName;
            TelemetryJson = telemetryJson;
        }

        public string Token { get; }
        public string UserID { get; }
        public string SessionID { get; }
        public int SchemaVersion { get; }
        public string FileName { get; }
        public string TelemetryJson { get; }
    }

    public readonly struct SurveyUploadResult
    {
        private SurveyUploadResult(bool succeeded, bool alreadyExists, string fileId, string fileUrl, string error)
        {
            Succeeded = succeeded;
            AlreadyExists = alreadyExists;
            FileID = fileId ?? string.Empty;
            FileURL = fileUrl ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public bool Succeeded { get; }
        public bool AlreadyExists { get; }
        public string FileID { get; }
        public string FileURL { get; }
        public string Error { get; }

        public static SurveyUploadResult Success(bool alreadyExists, string fileId, string fileUrl) =>
            new(true, alreadyExists, fileId, fileUrl, string.Empty);

        public static SurveyUploadResult Failure(string error) =>
            new(false, false, string.Empty, string.Empty, error);
    }

    public enum SurveySubmissionOutcome
    {
        Opened,
        NoCompletedSession,
        MissingToken,
        InvalidArtifact,
        PersistenceFailed,
        UploadFailed,
        OpenFailed
    }

    [Serializable]
    internal sealed class GeneratedUploadConfig
    {
        public string UploadToken;
    }
}
