﻿using GitLabToAzureDevOpsMigrator.Domain.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces;

public interface IAzureDevOpsWorkItemBl
{
    Task<List<WorkItem>> GetAllWorkItems();
    Task<List<Ticket>> CreateWorkItems(List<Ticket> tickets);
}