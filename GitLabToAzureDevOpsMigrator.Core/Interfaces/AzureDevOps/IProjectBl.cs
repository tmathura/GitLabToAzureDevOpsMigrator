using Microsoft.TeamFoundation.Core.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface IProjectBl
{
    Task<TeamProjectReference?> Get(string projectName);
}