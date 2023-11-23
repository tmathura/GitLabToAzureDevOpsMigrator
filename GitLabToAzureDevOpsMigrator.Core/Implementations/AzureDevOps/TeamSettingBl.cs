using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using log4net;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.AzureDevOps;

public class TeamSettingBl : ITeamSettingBl
{
    private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IConsoleHelper ConsoleHelper { get; }
    private WorkHttpClient WorkHttpClient { get; }

    public TeamSettingBl(IConsoleHelper consoleHelper, IVssConnection vssConnection)
    {
        ConsoleHelper = consoleHelper;

        var workHttpClient = vssConnection.GetClient<WorkHttpClient>();

        WorkHttpClient = workHttpClient ?? throw new Exception("WorkHttpClient is null.");
    }

    public async Task UpdateIterations(Guid projectId, List<Cycle>? cycles, WebApiTeam webApiTeam)
    {
        var count = 0;
        var errorCount = 0;

        if (cycles == null || cycles.Count == 0)
        {
            const string noCyclesMessage = "Updating Azure DevOps team iterations encountered a problem, no iterations to update from.";

            Console.WriteLine($"{Environment.NewLine}{noCyclesMessage}");
            Logger.Info(noCyclesMessage);

            return;
        }

        var startingProcessMessage = $"Started updating Azure DevOps team iterations, there are {cycles.Count(cycle => cycle.Iteration != null)} Azure DevOps team iterations to update from.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        foreach (var cycle in cycles)
        {
            try
            {
                count++;

                if (cycle.Iteration != null)
                {
                    var teamSettingsIteration = new TeamSettingsIteration
                    {
                        Id = cycle.Iteration.Identifier,
                    };

                    var teamContext = new TeamContext(projectId, webApiTeam.Id);

                    _ = await WorkHttpClient.PostTeamIterationAsync(teamSettingsIteration, teamContext);

                    Logger.Info($"Updated {count} Azure DevOps team iterations so far, team {webApiTeam.Name} iteration {cycle.Iteration.Id} - '{cycle.Iteration.Name}' was added.");
                }

                ConsoleHelper.DrawConsoleProgressBar(cycles.Count);
            }
            catch (Exception exception)
            {
                errorCount++;

                if (cycle.Iteration == null)
                {
                    Logger.Error($"Error updating Azure DevOps team iteration for iteration #{cycle.Milestone.Id} - '{cycle.Milestone.Title}', was on team iteration update count: {count}.", exception);
                }
                else
                {
                    Logger.Error($"Error updating Azure DevOps team iteration for iteration #{cycle.Iteration.Id} - '{cycle.Iteration.Name}', was on team iteration update count: {count}.", exception);
                }
            }
        }

        var endingProcessMessage = $"Finished updating Azure DevOps team iterations, there were {count} iterations added to team {webApiTeam.Name} & there were errors creating {errorCount} iterations.";

        Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
        Logger.Info(endingProcessMessage);
    }
}