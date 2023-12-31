﻿using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using log4net;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using static System.DateTime;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.AzureDevOps;

public class IterationBl : IIterationBl
{
    private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IConsoleHelper ConsoleHelper { get; }
    private WorkItemTrackingHttpClient WorkItemTrackingHttpClient { get; }

    public IterationBl(IConsoleHelper consoleHelper, IVssConnection vssConnection)
    {
        ConsoleHelper = consoleHelper;
        WorkItemTrackingHttpClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();
    }

    public async Task<List<Cycle>?> Create(string projectName, List<Cycle>? cycles)
    {
        var count = 0;
        var errorCount = 0;

        if (cycles == null || cycles.Count == 0)
        {
            const string noCyclesMessage = "Creating Azure DevOps iterations encountered a problem, no milestones to create from.";

            Console.WriteLine($"{Environment.NewLine}{noCyclesMessage}");
            Logger.Info(noCyclesMessage);

            return null;
        }

        var startingProcessMessage = $"Started creating Azure DevOps iterations, there are {cycles.Count} GitLab milestones to create from.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        foreach (var cycle in cycles.OrderBy(GetCycleStartDate))
        {
            try
            {
                count++;

                var workItemClassificationNode = new WorkItemClassificationNode
                {
                    StructureType = TreeNodeStructureType.Iteration,
                    Name = CleanTitle(cycle.Milestone.Title)
                };

                _ = TryParse(cycle.Milestone.StartDate, out var startDate);
                _ = TryParse(cycle.Milestone.DueDate, out var dueDate);

                if (startDate != MinValue && dueDate != MinValue)
                {
                    workItemClassificationNode.Attributes = new Dictionary<string, object>
                    {
                        { "startDate", startDate },
                        { "finishDate", dueDate },
                    };
                }

                var createdWorkItemClassificationNode = await WorkItemTrackingHttpClient.CreateOrUpdateClassificationNodeAsync(workItemClassificationNode, projectName, TreeStructureGroup.Iterations);
                    
                cycle.Iteration = createdWorkItemClassificationNode;
                    
                Logger.Info($"Created {count} Azure DevOp iterations so far, iteration {createdWorkItemClassificationNode.Id} - '{cycle.Milestone.Title}' was just created.");

                ConsoleHelper.DrawConsoleProgressBar(cycles.Count);
            }
            catch (Exception exception)
            {
                errorCount++;

                Logger.Error($"Error creating Azure DevOps iteration for milestone {cycle.Milestone.Id} - '{cycle.Milestone.Title}', was on iteration count: {count}.", exception);
            }
        }

        ConsoleHelper.ResetProgressBar();

        var endingProcessMessage = $"Finished creating Azure DevOps iterations, there were {cycles.Count(cycle => cycle.Iteration != null)} iterations created & there were errors creating {errorCount} iterations.";

        Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
        Logger.Info(endingProcessMessage);

        return cycles;
    }

    private static DateTime GetCycleStartDate(Cycle cycle)
    {
        if (TryParse(cycle.Milestone.StartDate, out var startDate) && startDate != MinValue)
        {
            return startDate;
        }
        return MinValue;
    }

    private static string CleanTitle(string title)
    {
        char[] charactersToRemove = { '\\', '/', ':', '*', '?', '"', '<', '>', '|', ';', '#', '$', '{', '}', ',', '+', '=', '[', ']' };

        var cleanedTitle =  charactersToRemove.Aggregate(title, (current, character) => current.Replace(character.ToString(), string.Empty));

        return cleanedTitle;
    }
}