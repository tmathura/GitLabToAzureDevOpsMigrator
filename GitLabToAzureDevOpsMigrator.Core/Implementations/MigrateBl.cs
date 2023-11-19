using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class MigrateBl : IMigrateBl
    {
        private IMilestoneBl MilestoneBl { get; }
        private IIterationBl IterationBl { get; }
        private IIssueBl IssueBl { get; }
        private IWorkItemBl AzureDevOpsWorkItemBl { get; }

        public MigrateBl(IMilestoneBl milestoneBl, IIterationBl iterationBl, IIssueBl issueBl, IWorkItemBl azureDevOpsWorkItemBl)
        {
            MilestoneBl = milestoneBl;
            IterationBl = iterationBl;
            IssueBl = issueBl;
            AzureDevOpsWorkItemBl = azureDevOpsWorkItemBl;
        }
        
        public async Task Migrate()
        {
            var cycles = MilestoneBl.Get();

            if (cycles != null)
            {
                await IterationBl.Create(cycles);
            }

            var tickets = await IssueBl.Get();

            if (tickets != null)
            {
                await AzureDevOpsWorkItemBl.CreateWorkItems(cycles, tickets);
            }
        }
    }
}