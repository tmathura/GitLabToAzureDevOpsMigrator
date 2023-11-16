using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces;

public interface IGitLabIssueBl
{
    Task<List<FullIssueDetails>?> CollectIssues();
}