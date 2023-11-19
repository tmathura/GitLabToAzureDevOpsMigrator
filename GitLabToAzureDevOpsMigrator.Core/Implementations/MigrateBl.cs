using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class MigrateBl : IMigrateBl
    {
        private IMilestoneBl MilestoneBl { get; }
        private IIterationBl IterationBl { get; }
        private IGitLabIssueBl GitLabIssueBl { get; }
        private IAzureDevOpsWorkItemBl AzureDevOpsWorkItemBl { get; }

        public MigrateBl(IMilestoneBl milestoneBl, IIterationBl iterationBl, IGitLabIssueBl gitLabIssueBl, IAzureDevOpsWorkItemBl azureDevOpsWorkItemBl)
        {
            MilestoneBl = milestoneBl;
            IterationBl = iterationBl;
            GitLabIssueBl = gitLabIssueBl;
            AzureDevOpsWorkItemBl = azureDevOpsWorkItemBl;
        }
        
        public async Task Migrate()
        {
            var cycles = MilestoneBl.Get();

            if (cycles != null)
            {
                await IterationBl.Create(cycles);
            }

            var tickets = await GitLabIssueBl.CollectIssues();

            if (tickets != null)
            {
                await AzureDevOpsWorkItemBl.CreateWorkItems(tickets);
            }
        }
    }
}