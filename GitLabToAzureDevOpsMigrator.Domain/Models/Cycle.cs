using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models;

public class Cycle
{
    public Cycle(Milestone milestone, WorkItemClassificationNode? iteration)
    {
        Milestone = milestone;
        Iteration = iteration;
    }

    public Milestone Milestone { get; set; }

    public WorkItemClassificationNode? Iteration { get; set; }
}