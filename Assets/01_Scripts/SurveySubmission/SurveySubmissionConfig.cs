namespace Octoplug.SurveySubmission
{
    public static class SurveySubmissionConfig
    {
        public const string UploadEndpoint = "https://script.google.com/macros/s/AKfycbz7YFKqd7M889wWruor3DSam17Hp839n7UPb5Jh191V0FDHu9INjswR4fdv-Mn6LwH1nA/exec";
        public const string SurveyFormBaseUrl = "https://docs.google.com/forms/d/e/1FAIpQLSeWxaxSIjxEX9apLf7wQiq6pBkDWKJYM4ZAg6nQ4ypTCmDVzQ/viewform?usp=pp_url";
        public const string UserIdEntry = "entry.1392202199";
        public const string SessionIdEntry = "entry.480603338";
        public const string LogFileUrlEntry = "entry.1489638143";
        public const string LogSchemaVersionEntry = "entry.564784767";
        public const int UploadTimeoutSeconds = 30;
        public const string UploadTokenEnvironmentVariable = "OCTOPLUG_UPLOAD_TOKEN";
        public const string GeneratedConfigRelativePath = "OctoplugGenerated/telemetry-upload.json";
    }
}
