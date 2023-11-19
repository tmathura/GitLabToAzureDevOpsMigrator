﻿using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Models.Settings;
using Microsoft.Extensions.Configuration;
using NGitLab;
using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.GitLab
{
    public class EpicBl : IEpicBl
    {
        private IGitLabClient GitLabClient { get; }
        private GitLabSettings GitLabSettings { get; }

        public EpicBl(IConfiguration configuration, IGitLabClient gitLabClient)
        {
            GitLabClient = gitLabClient;
            var appSettings = new AppSettings();
            configuration.Bind(appSettings);
            GitLabSettings = appSettings.GitLab;
        }

        public void Get()
        {
            var epics = GitLabClient.Epics.Get(GitLabSettings.GroupId, new EpicQuery()).ToList();

            Console.WriteLine(epics.Count == 0 ? "No epics found." : $"There are {epics.Count} epics.");

            foreach (var epic in epics)
            {
                Console.WriteLine($"This is epic {epic.EpicIid} - '{epic.Title}'");
            }
        }
    }
}