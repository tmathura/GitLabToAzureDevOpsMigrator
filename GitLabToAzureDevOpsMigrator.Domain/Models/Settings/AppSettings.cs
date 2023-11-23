namespace GitLabToAzureDevOpsMigrator.Domain.Models.Settings;

public class AppSettings
{
    public GitLabSettings GitLab { get; set; }
    public AzureDevOpsSettings AzureDevOps { get; set; }
}