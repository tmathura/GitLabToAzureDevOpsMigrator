using GitLabToAzureDevOpsMigrator.Domain.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces;

public interface IGitLabIssueBl
{
    Task<List<Ticket>?> CollectIssues();
}