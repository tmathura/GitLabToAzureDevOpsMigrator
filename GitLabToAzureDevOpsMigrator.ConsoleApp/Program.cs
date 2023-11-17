using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Implementations;
using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Implementations;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Domain.Models.Settings;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Implementations;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common;
using NGitLab;
using NGitLab.Impl;
using RestSharp;
using System.Diagnostics;
using System.Reflection;

namespace GitLabToAzureDevOpsMigrator.ConsoleApp
{
    internal class Program
    {
        private static ILog Logger { get; } = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        private static async Task Main()
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var appSettings = new AppSettings();
            configuration.Bind(appSettings);

            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            var fileInfo = new FileInfo("log4net.config");
            log4net.Config.XmlConfigurator.Configure(repository, fileInfo);

            var restClient = new RestClient($"{appSettings.GitLab.Url}");
            restClient.AddDefaultHeader("PRIVATE-TOKEN", appSettings.GitLab.AccessToken);

            var vssBasicCredential = new VssBasicCredential(appSettings.AzureDevOps.AccessToken, string.Empty);
            
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration, ConfigurationRoot>(_ => (ConfigurationRoot)configuration)
                .AddSingleton<IRestClient, RestClient>(_ => restClient)
                .AddSingleton<IProjectService, ProjectService>()
                .AddSingleton<IGitLabClient, GitLabClient>(_ => new GitLabClient(appSettings.GitLab.Url, appSettings.GitLab.AccessToken))
                .AddSingleton<IProjectIssueNoteClient, ProjectIssueNoteClient>(services => (ProjectIssueNoteClient)services.GetRequiredService<IGitLabClient>().GetProjectIssueNoteClient(appSettings.GitLab.ProjectId))
                .AddSingleton<IGitLabIssueBl, GitLabIssueBl>()
                .AddSingleton<IVssConnection, VssConnectionWrapper>(_ => new VssConnectionWrapper(new Uri(appSettings.AzureDevOps.Url), vssBasicCredential))
                .AddSingleton<IAzureDevOpsWorkItemBl, AzureDevOpsWorkItemBl>()
                .AddSingleton<IMigrateBl, MigrateBl>()
                .BuildServiceProvider();

            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var migrateBl = serviceProvider.GetRequiredService<IMigrateBl>();

                await migrateBl.Migrate();

                stopwatch.Stop();

                var elapsed = stopwatch.Elapsed;
                var elapsedTimeMessage = $"Time taken to migrate: {elapsed.Hours} hours, {elapsed.Minutes} minutes, {elapsed.Seconds} seconds, {elapsed.Milliseconds} milliseconds";
                
                Console.WriteLine(elapsedTimeMessage);
                Logger.Info(elapsedTimeMessage);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                Console.WriteLine($"{Environment.NewLine}An exception occurred, check the logs for information.");
                Console.WriteLine(exception.Message);
            }

            Console.ReadLine();
        }
    }
}