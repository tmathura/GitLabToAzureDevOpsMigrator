using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;

public class Team
{
    public Team(WebApiTeam webApiTeam)
    {
        WebApiTeam = webApiTeam;
    }

    public WebApiTeam WebApiTeam { get; set; }
    public List<TeamMember> TeamMembers { get; set; } = new();
}