using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;
using log4net;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

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
                        Id = cycle.Iteration.Identifier
                    };

                    var teamContext = new TeamContext(projectId, webApiTeam.Id);

                    _ = await WorkHttpClient.PostTeamIterationAsync(teamSettingsIteration, teamContext);

                    Logger.Info($"Updated {count} Azure DevOps team iterations so far, team {webApiTeam.Name} iteration {cycle.Iteration.Id} - '{cycle.Iteration.Name}' was added.");
                }

                ConsoleHelper.DrawConsoleProgressBar(count);
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

    public async Task UpdateAreas(Guid projectId, List<Team>? teams, List<WorkItemClassificationNode>? areas)
    {
        var count = 0;
        var errorCount = 0;

        if (teams == null || teams.Count == 0 || areas == null || areas.Count == 0)
        {
            const string noCyclesMessage = "Updating Azure DevOps team areas encountered a problem, no teams or areas to update from.";

            Console.WriteLine($"{Environment.NewLine}{noCyclesMessage}");
            Logger.Info(noCyclesMessage);

            return;
        }

        var startingProcessMessage = $"Started updating Azure DevOps team areas, there are {teams.Count} Azure DevOps teams to update.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        foreach (var team in teams)
        {
            try
            {
                count++;

                var teamContext = new TeamContext(projectId, team.WebApiTeam.Id);

                var currentTeamFieldValues = await WorkHttpClient.GetTeamFieldValuesAsync(teamContext);

                if (currentTeamFieldValues != null)
                {
                    var newTeamFieldValues = new List<TeamFieldValue>(currentTeamFieldValues.Values);

                    var foundTeamAreas = areas.FindAll(area => area.Name.Contains(team.WebApiTeam.Name));

                    var areasNotInCurrentTeamFieldValues = foundTeamAreas.FindAll(foundArea => !currentTeamFieldValues.Values.Any(value => value.Value.Contains(foundArea.Name)));

                    if (areasNotInCurrentTeamFieldValues.Count == 0)
                    {
                        Logger.Info($"Team {team.WebApiTeam.Name} areas already up to date.");
                    }
                    else
                    {
                        newTeamFieldValues.AddRange(areasNotInCurrentTeamFieldValues.Select(area => new TeamFieldValue { Value = area.Path.Replace(@"Area\", string.Empty), IncludeChildren = false }));

                        var teamAreasPatch = new TeamFieldValuesPatch
                        {
                            DefaultValue = currentTeamFieldValues.DefaultValue,
                            Values = newTeamFieldValues
                        };

                        var updatedTeamFieldValues = await WorkHttpClient.UpdateTeamFieldValuesAsync(teamAreasPatch, teamContext);

                        Logger.Info($"Updated {count} Azure DevOps team areas so far, team {team.WebApiTeam.Name} had {areasNotInCurrentTeamFieldValues.Count} new areas added.");
                    }

                }

                ConsoleHelper.DrawConsoleProgressBar(count);
            }
            catch (Exception exception)
            {
                errorCount++;

                Logger.Error($"Error updating Azure DevOps team {team.WebApiTeam.Name} areas, was on team update count: {count}.", exception);
            }
        }

        var endingProcessMessage = $"Finished updating Azure DevOps team areas, there were {count} new areas added to the teams & there were errors creating {errorCount} iterations.";

        Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
        Logger.Info(endingProcessMessage);
    }
}