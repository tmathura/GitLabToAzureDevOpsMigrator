namespace GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps
{
    public class AzureDevOpsSettings
    {
        public string Url { get; set; }
        public string AccessToken { get; set; }
        public string Project { get; set; }
    }
}
