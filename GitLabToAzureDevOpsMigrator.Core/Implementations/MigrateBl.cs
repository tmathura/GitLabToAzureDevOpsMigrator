using GitLabToAzureDevOpsMigrator.Core.Interfaces;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class MigrateBl : IMigrateBl
    {
        private IGitLabIssueBl GitLabIssueBl { get; }
        private IAzureDevOpsWorkItemBl AzureDevOpsWorkItemBl { get; }

        public MigrateBl(IGitLabIssueBl gitLabIssueBl, IAzureDevOpsWorkItemBl azureDevOpsWorkItemBl)
        {
            GitLabIssueBl = gitLabIssueBl;
            AzureDevOpsWorkItemBl = azureDevOpsWorkItemBl;
        }
        
        public async Task Migrate()
        {
            var tickets = await GitLabIssueBl.CollectIssues();
            if (tickets != null)
            {
                await AzureDevOpsWorkItemBl.CreateWorkItems(tickets);
            }
        }
    }
}