using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using log4net;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.AzureDevOps;

public class RepositoryBl : IRepositoryBl
{
    private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IConsoleHelper ConsoleHelper { get; }
    private GitHttpClient GitHttpClient { get; }

    public RepositoryBl(IConsoleHelper consoleHelper, IVssConnection vssConnection)
    {
        ConsoleHelper = consoleHelper;

        var gitHttpClient = vssConnection.GetClient<GitHttpClient>();

        GitHttpClient = gitHttpClient ?? throw new Exception("GitHttpClient is null.");
    }

    public async Task<GitRepository?> Get(Guid projectId, string repositoryName)
    {
        const string startingProcessMessage = "Started getting Azure DevOps repository.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        GitRepository? repository = null;

        try
        {
            var repositories = await GitHttpClient.GetRepositoriesAsync(projectId.ToString());
            repository = repositories.First(x => x.Name == repositoryName);

            ConsoleHelper.DrawConsoleProgressCount(1);
        }
        catch (Exception exception)
        {
            Logger.Error($"Getting Azure DevOps repository encountered a problem: {exception.Message}", exception);

            throw;
        }

        const string endingProcessMessage = "Finished getting Azure DevOps repository.";

        Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
        Logger.Info(endingProcessMessage);

        return repository;
    }
}