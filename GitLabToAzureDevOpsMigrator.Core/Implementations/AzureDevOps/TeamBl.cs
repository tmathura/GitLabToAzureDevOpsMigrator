using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;
using log4net;
using Microsoft.TeamFoundation.Core.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.AzureDevOps;

public class TeamBl : ITeamBl
{
    private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IConsoleHelper ConsoleHelper { get; }
    private TeamHttpClient TeamHttpClient { get; }

    public TeamBl(IConsoleHelper consoleHelper, IVssConnection vssConnection)
    {
        ConsoleHelper = consoleHelper;

        var teamHttpClient = vssConnection.GetClient<TeamHttpClient>();

        TeamHttpClient = teamHttpClient ?? throw new Exception("TeamHttpClient is null.");
    }

    public async Task<List<Team>?> GetTeams(Guid projectId)
    {
        const string startingProcessMessage = "Started getting Azure DevOps teams & team members.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        try
        {
            var count = 0;
            var errorCount = 0;
            var teams = new List<Team>();
            var webApiTeams = await TeamHttpClient.GetTeamsAsync(projectId.ToString());

            foreach (var webApiTeam in webApiTeams)
            {

                try
                {
                    count++;

                    var team = new Team(webApiTeam);

                    var teamMembers = await TeamHttpClient.GetTeamMembersWithExtendedPropertiesAsync(projectId.ToString(), webApiTeam.Id.ToString());

                    if (teamMembers != null)
                    {
                        team.TeamMembers.AddRange(teamMembers);
                    }

                    teams.Add(team);

                    ConsoleHelper.DrawConsoleProgressCount(teams.Sum(x => x.TeamMembers.Count));
                }
                catch (Exception exception)
                {
                    errorCount++;

                    Logger.Error($"Error getting Azure DevOps team members for team '{webApiTeam.Name}', was on team count: {count}.", exception);
                }
            }

            var endingProcessMessage = $"Finished getting Azure DevOps teams & team members , there were {teams.Count} teams & {teams.Sum(x => x.TeamMembers.Count)} team members retrieved & there were errors getting {errorCount} team members.";

            Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
            Logger.Info(endingProcessMessage);

            return teams;
        }
        catch (Exception exception)
        {
            Logger.Error($"Getting Azure DevOps teams & team members encountered a problem: {exception.Message}", exception);

            return null;
        }
    }
}