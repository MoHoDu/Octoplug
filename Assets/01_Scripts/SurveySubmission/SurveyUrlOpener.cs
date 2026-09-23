using System;
using UnityEngine;

namespace Octoplug.SurveySubmission
{
    public interface ISurveyUrlOpener
    {
        void Open(string url);
    }

    public sealed class ApplicationSurveyUrlOpener : ISurveyUrlOpener
    {
        public void Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Survey URL is required.", nameof(url));
            }

            Application.OpenURL(url);
        }
    }
}
