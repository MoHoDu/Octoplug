using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Octoplug.SurveySubmission
{
    public interface ISurveyUploadTransport
    {
        IEnumerator Upload(SurveyUploadRequest request, Action<SurveyUploadResult> completed);
    }

    public sealed class UnityWebRequestSurveyUploadTransport : ISurveyUploadTransport
    {
        public IEnumerator Upload(SurveyUploadRequest request, Action<SurveyUploadResult> completed)
        {
            if (!SurveyUploadPayloadBuilder.TryBuild(request, out var payload, out var buildError))
            {
                completed(SurveyUploadResult.Failure(buildError));
                yield break;
            }

            using var webRequest = new UnityWebRequest(SurveySubmissionConfig.UploadEndpoint, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = SurveySubmissionConfig.UploadTimeoutSeconds;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                completed(SurveyUploadResult.Failure($"Upload request failed ({webRequest.responseCode})."));
                yield break;
            }

            completed(SurveyUploadResponseParser.Parse(webRequest.downloadHandler.text));
        }
    }

    public static class SurveyUploadResponseParser
    {
        [Serializable]
        private sealed class UploadResponse
        {
            public bool success;
            public bool already_exists;
            public string file_id;
            public string file_url;
        }

        public static SurveyUploadResult Parse(string json)
        {
            UploadResponse response;
            try
            {
                response = JsonUtility.FromJson<UploadResponse>(json);
            }
            catch
            {
                return SurveyUploadResult.Failure("Upload response was not valid JSON.");
            }

            if (response == null || !response.success)
            {
                return SurveyUploadResult.Failure("Upload endpoint reported failure.");
            }

            if (!Uri.TryCreate(response.file_url, UriKind.Absolute, out var fileUri) ||
                !string.Equals(fileUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return SurveyUploadResult.Failure("Upload response did not contain a valid HTTPS file URL.");
            }

            return SurveyUploadResult.Success(response.already_exists, response.file_id, response.file_url);
        }
    }

    public static class SurveyUploadPayloadBuilder
    {
        [Serializable]
        private sealed class TelemetryEnvelope
        {
            public bool IsFinalized;
        }

        public static bool TryBuild(SurveyUploadRequest request, out string payload, out string error)
        {
            payload = string.Empty;
            error = string.Empty;
            TelemetryEnvelope document;
            try
            {
                document = JsonUtility.FromJson<TelemetryEnvelope>(request.TelemetryJson);
            }
            catch
            {
                error = "Finalized telemetry artifact was not valid JSON.";
                return false;
            }

            if (document == null || !document.IsFinalized)
            {
                error = "Only finalized telemetry artifacts can be uploaded.";
                return false;
            }

            payload = "{" +
                      "\"upload_token\":\"" + Escape(request.Token) + "\"," +
                      "\"user_id\":\"" + Escape(request.UserID) + "\"," +
                      "\"session_id\":\"" + Escape(request.SessionID) + "\"," +
                      "\"schema_version\":" + request.SchemaVersion + "," +
                      "\"file_name\":\"" + Escape(request.FileName) + "\"," +
                      "\"telemetry_json\":" + request.TelemetryJson +
                      "}";
            return true;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
