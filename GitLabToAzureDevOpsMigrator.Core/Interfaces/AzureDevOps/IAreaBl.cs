using GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface IAreaBl
{
    Task<List<WorkItemClassificationNode>?> GetAreas(Guid projectId);
    Task<List<WorkItemClassificationNode>?> Create(Guid projectId, string projectName, List<Team>? teams);
}