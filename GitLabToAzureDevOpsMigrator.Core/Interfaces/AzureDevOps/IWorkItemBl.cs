using GitLabToAzureDevOpsMigrator.Domain.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface IWorkItemBl
{
    Task<WorkItem?> GetWorkItem(string projectName, int id);
    Task<List<WorkItem>> GetWorkItems(string projectName);
    Task FixWorkItemsArtifactLink(Guid projectId, Guid repositoryId);
    Task FixWorkItemArtifactLink(WorkItem workItem, Guid projectId, Guid repositoryId);
    Task<List<Ticket>?> CreateWorkItems(Guid projectId, Guid repositoryId, List<Cycle>? cycles, List<Ticket>? tickets , List<TeamMember> teamMembers, WorkItemClassificationNode? defaultArea);
}