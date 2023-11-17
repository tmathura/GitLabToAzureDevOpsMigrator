namespace GitLabToAzureDevOpsMigrator.Domain.Models.Settings
{
    public class AzureDevOpsSettings
    {
        public string Url { get; set; }
        public string AccessToken { get; set; }
        public string ProjectName { get; set; }
        public string DefaultIterationPath { get; set; }
    }
}
