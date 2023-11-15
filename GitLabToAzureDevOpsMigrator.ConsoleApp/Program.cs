using GitLabToAzureDevOpsMigrator.Core.Implementations;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Implementations;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using System.Diagnostics;
using System.Reflection;

namespace GitLabToAzureDevOpsMigrator.ConsoleApp
{
    internal class Program
    {
        private static ILog Logger { get; } = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        private static async Task Main(string[] args)
        {
            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            var fileInfo = new FileInfo("log4net.config");
            log4net.Config.XmlConfigurator.Configure(repository, fileInfo);

            const string gitLabUrl = "https://gitlab.com";
            const string gitLabAccessToken = "";

            var restClient = new RestClient($"{gitLabUrl}/api/v4");
            restClient.AddDefaultHeader("PRIVATE-TOKEN", gitLabAccessToken);

            var serviceProvider = new ServiceCollection()
                .AddSingleton<IRestClient, RestClient>(_ => restClient)
                .AddSingleton<IProjectService, ProjectService>()
                .AddSingleton<IMigratorBl, MigratorBl>()
                .BuildServiceProvider();
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var migratorBl = serviceProvider.GetService<IMigratorBl>();

                if (migratorBl == null)
                {
                    throw new Exception("The console app migrator is null.");
                }

                await migratorBl.CollectGitLabIssues(gitLabUrl, 0, 0, gitLabAccessToken);

                stopwatch.Stop();

                var elapsed = stopwatch.Elapsed;
                var elapsedTimeMessage = $"Time taken to migrate: {elapsed.Hours} hours, {elapsed.Minutes} minutes, {elapsed.Seconds} seconds, {elapsed.Milliseconds} milliseconds";
                
                Console.WriteLine(elapsedTimeMessage);
                Logger.Info(elapsedTimeMessage);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                Console.WriteLine("An exception occurred, check the logs for information.");
                Console.WriteLine(exception.Message);
            }

            Console.ReadLine();
        }
    }
}