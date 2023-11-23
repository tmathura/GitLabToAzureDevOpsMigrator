using GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface ITeamBl
{
    Task<List<Team>?> GetTeams(Guid projectId);
}