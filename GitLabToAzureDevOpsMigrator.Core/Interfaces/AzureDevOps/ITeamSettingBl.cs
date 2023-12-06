using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface ITeamSettingBl
{
    Task UpdateIterations(Guid projectId, List<Cycle> cycles, WebApiTeam webApiTeam);
    Task UpdateAreas(Guid projectId, List<Team>? teams, List<WorkItemClassificationNode>? areas);
}