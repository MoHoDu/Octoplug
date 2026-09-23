using System;
using System.IO;
using Octoplug.SurveySubmission;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Octoplug.Editor.SurveySubmission
{
    public sealed class SurveySubmissionBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string DevelopmentBuildPath = "Builds/Windows/Octoplug.exe";

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows &&
                report.summary.platform != BuildTarget.StandaloneWindows64)
            {
                return;
            }

            RemoveGeneratedConfig();
            if (!EditorUploadTokenProvider.TryGet(out var token))
            {
                throw new BuildFailedException("OCTOPLUG_UPLOAD_TOKEN is not configured for the Windows build.");
            }

            WriteGeneratedConfig(token);
            token = string.Empty;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            RemoveGeneratedConfig();
        }

        [MenuItem("Octoplug/Build/Development Windows Player")]
        public static void BuildDevelopmentWindowsPlayer()
        {
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = EnabledScenes(),
                    locationPathName = DevelopmentBuildPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development | BuildOptions.DetailedBuildReport
                };
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException($"Windows build finished with result {report.summary.result}.");
                }
            }
            finally
            {
                RemoveGeneratedConfig();
            }
        }

        internal static string GeneratedConfigPath => Path.Combine(
            Application.dataPath,
            "StreamingAssets",
            SurveySubmissionConfig.GeneratedConfigRelativePath.Replace('/', Path.DirectorySeparatorChar));

        internal static void RemoveGeneratedConfig()
        {
            try
            {
                if (File.Exists(GeneratedConfigPath)) File.Delete(GeneratedConfigPath);
                if (File.Exists(GeneratedConfigPath + ".meta")) File.Delete(GeneratedConfigPath + ".meta");
                DeleteEmptyParents(Path.GetDirectoryName(GeneratedConfigPath));
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Generated telemetry upload config cleanup failed: {exception.Message}");
            }
        }

        private static void WriteGeneratedConfig(string token)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GeneratedConfigPath));
            File.WriteAllText(GeneratedConfigPath, JsonUtility.ToJson(new GeneratedConfigDto { UploadToken = token }));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static string[] EnabledScenes()
        {
            return Array.ConvertAll(
                Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled),
                scene => scene.path);
        }

        private static void DeleteEmptyParents(string directory)
        {
            var streamingAssets = Path.Combine(Application.dataPath, "StreamingAssets");
            while (!string.IsNullOrEmpty(directory) &&
                   directory.StartsWith(streamingAssets, StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(directory, streamingAssets, StringComparison.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(directory) || Directory.GetFileSystemEntries(directory).Length != 0) break;
                Directory.Delete(directory);
                if (File.Exists(directory + ".meta")) File.Delete(directory + ".meta");
                directory = Path.GetDirectoryName(directory);
            }
        }

        [Serializable]
        private sealed class GeneratedConfigDto
        {
            public string UploadToken;
        }
    }

    public static class EditorUploadTokenProvider
    {
        public static bool TryGet(out string token)
        {
            token = Environment.GetEnvironmentVariable(SurveySubmissionConfig.UploadTokenEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(token))
            {
                token = token.Trim();
                return true;
            }

            try
            {
                var path = Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty, ".env.local");
                if (!File.Exists(path))
                {
                    token = string.Empty;
                    return false;
                }

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
    }
}
