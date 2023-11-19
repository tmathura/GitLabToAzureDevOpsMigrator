using GitLabToAzureDevOpsMigrator.Domain.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface IWorkItemBl
{
    Task<List<WorkItem>> GetAllWorkItems();
    Task<List<Ticket>?> CreateWorkItems(List<Ticket>? tickets);
}