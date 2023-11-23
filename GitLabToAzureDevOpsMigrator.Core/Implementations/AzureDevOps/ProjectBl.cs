using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using log4net;
using Microsoft.TeamFoundation.Core.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.AzureDevOps;

public class ProjectBl : IProjectBl
{
    private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IConsoleHelper ConsoleHelper { get; }
    private ProjectHttpClient ProjectHttpClient { get; }

    public ProjectBl(IConsoleHelper consoleHelper, IVssConnection vssConnection)
    {
        ConsoleHelper = consoleHelper;

        var projectHttpClient = vssConnection.GetClient<ProjectHttpClient>();

        ProjectHttpClient = projectHttpClient ?? throw new Exception("ProjectHttpClient is null.");
    }

    public async Task<TeamProjectReference?> Get(string projectName)
    {
        const string startingProcessMessage = "Started getting Azure DevOps project.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        TeamProjectReference? project = null;

        try
        {
            var projects = await ProjectHttpClient.GetProjects();
            project = projects.FirstOrDefault(x => x.Name == projectName);

            ConsoleHelper.DrawConsoleProgressCount(1);
        }
        catch (Exception exception)
        {
            Logger.Error($"Getting Azure DevOps project encountered a problem: {exception.Message}", exception);

            throw;
        }

        const string endingProcessMessage = "Finished getting Azure DevOps project.";

        Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
        Logger.Info(endingProcessMessage);

        return project;
    }
}