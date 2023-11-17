namespace GitLabToAzureDevOpsMigrator.Domain.Models.Settings
{
    public class GitLabSettings
    {
        public string Url { get; set; }
        public string AccessToken { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Cookie { get; set; }
        public string LabelToMigrate { get; set; }
    }
}
