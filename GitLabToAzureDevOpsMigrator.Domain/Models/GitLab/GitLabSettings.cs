namespace GitLabToAzureDevOpsMigrator.Domain.Models.GitLab
{
    public class GitLabSettings
    {
        public string Url { get; set; }
        public string ApiPath { get; set; }
        public string AccessToken { get; set; }
        public int GroupId { get; set; }
        public int ProjectId { get; set; }
    }
}
