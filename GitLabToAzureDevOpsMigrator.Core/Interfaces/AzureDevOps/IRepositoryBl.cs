using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface IRepositoryBl
{
    Task<GitRepository?> Get(Guid projectId, string repositoryName);
}