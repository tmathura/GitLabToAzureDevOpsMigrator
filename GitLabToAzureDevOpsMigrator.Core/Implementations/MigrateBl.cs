using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.Services.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations;

public class MigrateBl : IMigrateBl
{
    private AppSettings AppSettings { get; } = new();
    private IProjectBl ProjectBl { get; }
    private IRepositoryBl RepositoryBl { get; }
    private IMilestoneBl MilestoneBl { get; }
    private IIterationBl IterationBl { get; }
    private ITeamBl TeamBl { get; }
    private IAreaBl AreaBl { get; }
    private ITeamSettingBl TeamSettingBl { get; }
    private IEpicBl EpicBl { get; }
    private IIssueBl IssueBl { get; }
    private IWorkItemBl AzureDevOpsWorkItemBl { get; }

    public MigrateBl(IConfiguration configuration, IProjectBl projectBl, IRepositoryBl repositoryBl, IMilestoneBl milestoneBl, IIterationBl iterationBl, ITeamBl teamBl, IAreaBl areaBl, ITeamSettingBl teamSettingBl, IEpicBl epicBl, IIssueBl issueBl, IWorkItemBl azureDevOpsWorkItemBl)
    {
        configuration.Bind(AppSettings);

        ProjectBl = projectBl;
        RepositoryBl = repositoryBl;
        MilestoneBl = milestoneBl;
        IterationBl = iterationBl;
        TeamBl = teamBl;
        AreaBl = areaBl;
        TeamSettingBl = teamSettingBl;
        EpicBl = epicBl;
        IssueBl = issueBl;
        AzureDevOpsWorkItemBl = azureDevOpsWorkItemBl;
    }
        
    public async Task Migrate()
    {
        var project = await ProjectBl.Get(AppSettings.AzureDevOps.ProjectName) ?? throw new Exception("Getting Azure DevOps project encountered a problem, project is null.");
        var repository = await RepositoryBl.Get(project.Id, AppSettings.AzureDevOps.RepositoryName) ?? throw new Exception("Getting Azure DevOps repository encountered a problem, repository is null.");

        var cycles = MilestoneBl.Get();

        if (cycles != null)
        {
            await IterationBl.Create(AppSettings.AzureDevOps.ProjectName, cycles);
        }

        var teams = await TeamBl.GetTeams(project.Id);

        var areas = await AreaBl.GetAreas(project.Id);

        if (teams != null && areas != null)
        {
            var teamsWithoutAreaThatContainsTeamName = teams.FindAll(team => areas.All(area => area.Name != team.WebApiTeam.Name)).ToList();

            var createdAreas = await AreaBl.Create(project.Id, project.Name, teamsWithoutAreaThatContainsTeamName);

            if (createdAreas is { Count: > 0 })
            {
                areas.AddRange(createdAreas);
            }

            await TeamSettingBl.UpdateAreas(project.Id, teams, areas);
        }

        var team = teams?.FirstOrDefault(x => x.WebApiTeam.Name == AppSettings.AzureDevOps.DefaultTeamName);

        if (cycles != null && team != null)
        {
            await TeamSettingBl.UpdateIterations(project.Id, cycles, team.WebApiTeam);
        }

        var tickets = new List<Ticket>();

        var epicTickets = await EpicBl.Get(AppSettings.GitLab.GroupId, AppSettings.GitLab.GroupName, AppSettings.GitLab.LabelToMigrate);

        if (epicTickets != null)
        {
            tickets.AddRange(epicTickets);
        }

        var issueTickets = await IssueBl.Get(AppSettings.GitLab.GroupName, AppSettings.GitLab.ProjectId, AppSettings.GitLab.ProjectName, AppSettings.GitLab.LabelToMigrate);

        if (issueTickets != null)
        {
            tickets.AddRange(issueTickets);
        }

        var combinedTeamMembers = teams?.SelectMany(currentTeam => currentTeam.TeamMembers).ToList();

        var teamMembers = new List<TeamMember>();

        if (combinedTeamMembers != null)
        {
            teamMembers = combinedTeamMembers;
        }

        var defaultArea = areas?.FirstOrDefault(x => x.Name == AppSettings.AzureDevOps.DefaultArea);

        await AzureDevOpsWorkItemBl.CreateWorkItems(project.Id, repository.Id, cycles, tickets, teamMembers, defaultArea);
    }
}