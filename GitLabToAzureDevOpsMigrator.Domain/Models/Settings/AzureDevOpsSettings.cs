namespace GitLabToAzureDevOpsMigrator.Domain.Models.Settings;

public class AzureDevOpsSettings
{
    public string Url { get; set; }
    public string AccessToken { get; set; }
    public string ProjectName { get; set; }
    public string RepositoryName { get; set; }
    public string DefaultIterationPath { get; set; }
    public string DefaultTeamName { get; set; }
    public string DefaultArea { get; set; }
}