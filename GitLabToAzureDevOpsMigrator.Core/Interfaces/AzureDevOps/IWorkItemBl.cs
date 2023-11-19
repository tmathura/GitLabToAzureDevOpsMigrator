using GitLabToAzureDevOpsMigrator.Domain.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface IWorkItemBl
{
    Task<WorkItem?> GetWorkItem(int id);
    Task<List<WorkItem>> GetWorkItems();
    Task<List<Ticket>?> CreateWorkItems(List<Cycle>? cycles, List<Ticket>? tickets);
}