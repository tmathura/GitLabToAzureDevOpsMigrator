using GitLabToAzureDevOpsMigrator.Domain.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;

public interface IGitLabIssueBl
{
    Task<List<Ticket>?> CollectIssues();
}