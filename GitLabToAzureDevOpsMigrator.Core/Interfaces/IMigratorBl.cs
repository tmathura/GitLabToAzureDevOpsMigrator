namespace GitLabToAzureDevOpsMigrator.Core.Interfaces;

public interface IMigratorBl
{
    Task CollectGitLabIssues(string gitLabUrl, int gitLabGroupId, int gitLabProjectId, string gitLabAccessToken);
}