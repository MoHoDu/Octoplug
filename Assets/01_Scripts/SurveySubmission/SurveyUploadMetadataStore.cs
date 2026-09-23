using System;
using System.IO;
using UnityEngine;

namespace Octoplug.SurveySubmission
{
    public class SurveyUploadMetadataStore
    {
        private readonly string folder;

        public SurveyUploadMetadataStore(string persistentDataPath)
        {
            folder = Path.Combine(persistentDataPath, "OctoplugLogs", "survey-uploads");
        }

        public virtual SurveyUploadMetadata Load(string sessionId)
        {
            var empty = New(sessionId);
            try
            {
                var path = PathFor(sessionId);
                if (!File.Exists(path)) return empty;
                var metadata = JsonUtility.FromJson<SurveyUploadMetadata>(File.ReadAllText(path));
                if (metadata == null || !string.Equals(metadata.SessionID, sessionId, StringComparison.Ordinal)) return empty;
                if (metadata.UploadStatus == SurveyUploadStatus.Uploading)
                {
                    metadata.UploadStatus = SurveyUploadStatus.Failed;
                    metadata.LastError = "A previous upload did not complete.";
                    Save(metadata);
                }
                return metadata;
            }
            catch
            {
                return empty;
            }
        }

        public virtual bool Save(SurveyUploadMetadata metadata)
        {
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.SessionID)) return false;
            try
            {
                Directory.CreateDirectory(folder);
                var path = PathFor(metadata.SessionID);
                var temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(metadata, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string PathFor(string sessionId)
        {
            foreach (var character in Path.GetInvalidFileNameChars()) sessionId = sessionId.Replace(character, '_');
            return Path.Combine(folder, sessionId + ".json");
        }

        private static SurveyUploadMetadata New(string sessionId) => new()
        {
            SessionID = sessionId ?? string.Empty,
            UploadStatus = SurveyUploadStatus.NotUploaded,
            UploadedFileID = string.Empty,
            UploadedFileURL = string.Empty,
            UploadedAtUtc = string.Empty,
            LastError = string.Empty
        };
    }
}
