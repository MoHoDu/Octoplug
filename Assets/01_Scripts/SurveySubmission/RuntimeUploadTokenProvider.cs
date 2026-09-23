using System;
using System.IO;
using UnityEngine;

namespace Octoplug.SurveySubmission
{
    public static class RuntimeUploadTokenProvider
    {
        public static bool TryGet(out string token)
        {
            token = Environment.GetEnvironmentVariable(SurveySubmissionConfig.UploadTokenEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(token))
            {
                token = token.Trim();
                return true;
            }

#if UNITY_EDITOR
            if (TryReadEditorEnvironmentFile(out token))
            {
                return true;
            }
#endif

            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, SurveySubmissionConfig.GeneratedConfigRelativePath);
                if (!File.Exists(path))
                {
                    token = string.Empty;
                    return false;
                }

                var config = JsonUtility.FromJson<GeneratedUploadConfig>(File.ReadAllText(path));
                token = config?.UploadToken?.Trim();
                return !string.IsNullOrWhiteSpace(token);
            }
            catch
            {
                token = string.Empty;
                return false;
            }
        }

#if UNITY_EDITOR
        private static bool TryReadEditorEnvironmentFile(out string token)
        {
            token = string.Empty;
            try
            {
                var path = Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty, ".env.local");
                if (!File.Exists(path)) return false;

                foreach (var line in File.ReadAllLines(path))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal)) continue;
                    var separator = trimmed.IndexOf('=');
                    if (separator <= 0) continue;
                    if (!string.Equals(trimmed.Substring(0, separator).Trim(), SurveySubmissionConfig.UploadTokenEnvironmentVariable, StringComparison.Ordinal)) continue;
                    token = trimmed.Substring(separator + 1).Trim().Trim('"', '\'');
                    return !string.IsNullOrWhiteSpace(token);
                }
            }
            catch
            {
                token = string.Empty;
            }

            return false;
        }
#endif
    }
}
