using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;
using log4net;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.AzureDevOps;

public class AreaBl : IAreaBl
{
    private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IConsoleHelper ConsoleHelper { get; }
    private WorkItemTrackingHttpClient WorkItemTrackingHttpClient { get; }

    public AreaBl(IConsoleHelper consoleHelper, IVssConnection vssConnection)
    {
        ConsoleHelper = consoleHelper;

        var workItemTrackingHttpClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();

        WorkItemTrackingHttpClient = workItemTrackingHttpClient ?? throw new Exception("WorkItemTrackingHttpClient is null.");
    }

    public async Task<List<WorkItemClassificationNode>?> GetAreas(Guid projectId)
    {
        const string startingProcessMessage = "Started getting Azure DevOps areas.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        try
        {
            var count = 0;
            var areas = new List<WorkItemClassificationNode>();
            const int depth = 2;

            var workItemClassificationNode = await WorkItemTrackingHttpClient.GetClassificationNodeAsync(projectId, TreeStructureGroup.Areas, null, depth);

            if (workItemClassificationNode?.HasChildren != null && workItemClassificationNode.HasChildren.Value)
            {
                areas = workItemClassificationNode.Children.ToList();
                count = areas.Count;
            }

            ConsoleHelper.DrawConsoleProgressCount(count);

            var endingProcessMessage = $"Finished getting Azure DevOps areas, there were {count} areas retrieved.";

            Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
            Logger.Info(endingProcessMessage);

            return areas;
        }
        catch (Exception exception)
        {
            Logger.Error($"Getting Azure DevOps teams & team members encountered a problem: {exception.Message}", exception);

            return null;
        }
    }

    public async Task<List<WorkItemClassificationNode>?> Create(Guid projectId, string projectName, List<Team>? teams)
    {
        var count = 0;
        var errorCount = 0;
        var areas = new List<WorkItemClassificationNode>();

        if (teams == null || teams.Count == 0)
        {
            const string noTeamsMessage = "Creating Azure DevOps areas encountered a problem, no teams to create from.";

            Console.WriteLine($"{Environment.NewLine}{noTeamsMessage}");
            Logger.Info(noTeamsMessage);

            return null;
        }

        var startingProcessMessage = $"Started creating Azure DevOps areas, there are {teams.Count} Azure Devops teams to create from.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        foreach (var team in teams)
        {
            try
            {
                count++;
                
                var workItemClassificationNode = new WorkItemClassificationNode
                {
                    Name = team.WebApiTeam.Name,
                    StructureType = TreeNodeStructureType.Area
                };

                var createdWorkItemClassificationNode = await WorkItemTrackingHttpClient.CreateOrUpdateClassificationNodeAsync(workItemClassificationNode, projectId, TreeStructureGroup.Areas);

                areas.Add(createdWorkItemClassificationNode);

                Logger.Info($"Created {count} Azure DevOp areas so far, area {createdWorkItemClassificationNode.Id} - '{team.WebApiTeam.Name}' was just created.");

                ConsoleHelper.DrawConsoleProgressBar(count);
            }
            catch (Exception exception)
            {
                errorCount++;

                Logger.Error($"Error creating Azure DevOps area for team '{team.WebApiTeam.Name}', was on area count: {count}.", exception);
            }
        }

        var endingProcessMessage = $"Finished creating Azure DevOps areas, there were {areas.Count} areas created & there were errors creating {errorCount} iterations.";

        Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
        Logger.Info(endingProcessMessage);

        return areas;
    }
}