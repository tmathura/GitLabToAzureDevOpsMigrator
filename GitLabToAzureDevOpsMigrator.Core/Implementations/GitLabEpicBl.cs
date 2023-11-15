using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab;
using Microsoft.Extensions.Configuration;
using NGitLab;
using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class GitLabEpicBl : IGitLabEpicBl
    {
        private IGitLabClient GitLabClient { get; }
        private GitLabSettings GitLabSettings { get; }

        public GitLabEpicBl(IConfiguration configuration, IGitLabClient gitLabClient)
        {
            GitLabClient = gitLabClient;
            var appSettings = new AppSettings();
            configuration.Bind(appSettings);
            GitLabSettings = appSettings.GitLab;
        }

        public void CollectEpics()
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