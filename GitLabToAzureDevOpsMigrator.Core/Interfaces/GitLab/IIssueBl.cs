using GitLabToAzureDevOpsMigrator.Domain.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;

public interface IIssueBl
{
    Task<List<Ticket>?> Get(string groupName, int projectId, string projectName, string labelToMigrate);
}