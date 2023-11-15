using GitLabToAzureDevOpsMigrator.Core.Interfaces;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class MigrateBl : IMigrateBl
    {
        private IGitLabIssueBl GitLabIssueBl { get; }

        public MigrateBl(IGitLabIssueBl gitLabIssueBl)
        {
            GitLabIssueBl = gitLabIssueBl;
        }
        
        public async Task Migrate()
        {
            await GitLabIssueBl.CollectIssues();
        }
    }
}