using System;

namespace Octoplug.SurveySubmission
{
    public static class SurveyUrlBuilder
    {
        public static string Build(string userId, string sessionId, string fileUrl, int schemaVersion)
        {
            return SurveySubmissionConfig.SurveyFormBaseUrl +
                   "&" + SurveySubmissionConfig.UserIdEntry + "=" + Encode(userId) +
                   "&" + SurveySubmissionConfig.SessionIdEntry + "=" + Encode(sessionId) +
                   "&" + SurveySubmissionConfig.LogFileUrlEntry + "=" + Encode(fileUrl) +
                   "&" + SurveySubmissionConfig.LogSchemaVersionEntry + "=" + Encode(schemaVersion.ToString());
        }

        private static string Encode(string value) => Uri.EscapeDataString(value ?? string.Empty);
    }
}
