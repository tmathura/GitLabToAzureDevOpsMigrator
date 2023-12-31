﻿using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Implementations;
using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Implementations;
using GitLabToAzureDevOpsMigrator.Core.Implementations.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Core.Implementations.GitLab;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Interfaces;
using GitLabToAzureDevOpsMigrator.Domain.Models;
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
using RestSharp.Serializers.NewtonsoftJson;
using System.Reflection;

namespace GitLabToAzureDevOpsMigrator.ConsoleApp;

internal class Program
{
    private static ILog Logger { get; } = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

    private static async Task Main()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var appSettings = new AppSettings();
        configuration.Bind(appSettings);

        var loggerRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        var fileInfo = new FileInfo("log4net.config");
        log4net.Config.XmlConfigurator.Configure(loggerRepository, fileInfo);
            
        var restClient = new RestClient($"{appSettings.GitLab.Url}", configureSerialization: serializerConfig => serializerConfig.UseSerializer(() => new JsonNetSerializer()));
        restClient.AddDefaultHeader("PRIVATE-TOKEN", appSettings.GitLab.AccessToken);

        var vssBasicCredential = new VssBasicCredential(appSettings.AzureDevOps.AccessToken, string.Empty);
            
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration, ConfigurationRoot>(_ => (ConfigurationRoot)configuration)
            .AddSingleton<IStopwatchWrapper, StopwatchWrapper>()
            .AddTransient<IConsoleHelper, ConsoleHelper>()
            .AddSingleton<IProjectBl, ProjectBl>()
            .AddSingleton<IRepositoryBl, RepositoryBl>()
            .AddSingleton<IMilestoneBl, MilestoneBl>()
            .AddSingleton<IIterationBl, IterationBl>()
            .AddSingleton<ITeamBl, TeamBl>()
            .AddSingleton<IAreaBl, AreaBl>()
            .AddSingleton<ITeamSettingBl, TeamSettingBl>()
            .AddSingleton<IRestClient, RestClient>(_ => restClient)
            .AddSingleton<IGroupService, GroupService>()
            .AddSingleton<IProjectService, ProjectService>()
            .AddSingleton<IGitLabClient, GitLabClient>(_ => new GitLabClient(appSettings.GitLab.Url, appSettings.GitLab.AccessToken))
            .AddSingleton<IEpicClient, EpicClient>(services => services.GetRequiredService<IGitLabClient>().Epics as EpicClient ?? throw new Exception("IEpicClient is null."))
            .AddSingleton<IProjectIssueNoteClient, ProjectIssueNoteClient>(services => services.GetRequiredService<IGitLabClient>().GetProjectIssueNoteClient(appSettings.GitLab.ProjectId) as ProjectIssueNoteClient ?? throw new Exception("ProjectIssueNoteClient is null."))
            .AddSingleton<IIssueClient, IssueClient>(services => services.GetRequiredService<IGitLabClient>().Issues as IssueClient ?? throw new Exception("IssueClient is null."))
            .AddSingleton<IMilestoneClient, MilestoneClient>(services => services.GetRequiredService<IGitLabClient>().GetGroupMilestone(appSettings.GitLab.GroupId) as MilestoneClient ?? throw new Exception("MilestoneClient is null."))
            .AddSingleton<IMergeRequestClient, MergeRequestClient>(services => services.GetRequiredService<IGitLabClient>().GetMergeRequest(appSettings.GitLab.ProjectId) as MergeRequestClient ?? throw new Exception("MergeRequestClient is null."))
            .AddSingleton<IIssueBl, IssueBl>()
            .AddSingleton<IEpicBl, EpicBl>()
            .AddSingleton<IVssConnection, VssConnectionWrapper>(_ => new VssConnectionWrapper(new Uri(appSettings.AzureDevOps.Url), vssBasicCredential))
            .AddSingleton<IWorkItemBl, WorkItemBl>()
            .AddSingleton<IMigrateBl, MigrateBl>()
            .BuildServiceProvider();

        try
        {
            var stopwatch = serviceProvider.GetRequiredService<IStopwatchWrapper>();

            stopwatch.Start();

            var migrateBl = serviceProvider.GetRequiredService<IMigrateBl>();

            await migrateBl.Migrate();

            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed;
            var elapsedTimeMessage = $"Time taken to migrate: {elapsed.Hours} hours, {elapsed.Minutes} minutes, {elapsed.Seconds} seconds, {elapsed.Milliseconds} milliseconds";
                
            Console.WriteLine($"{Environment.NewLine}{elapsedTimeMessage}");
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