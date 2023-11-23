using GitLabToAzureDevOpsMigrator.Domain.Models;
using Microsoft.TeamFoundation.Core.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface ITeamSettingBl
{
    Task UpdateIterations(Guid projectId, List<Cycle> cycles, WebApiTeam webApiTeam);
}