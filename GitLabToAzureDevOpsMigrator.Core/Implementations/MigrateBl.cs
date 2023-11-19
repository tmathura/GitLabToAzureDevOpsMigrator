using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class MigrateBl : IMigrateBl
    {
        private IMilestoneBl MilestoneBl { get; }
        private IIterationBl IterationBl { get; }
        private IEpicBl EpicBl { get; }
        private IIssueBl IssueBl { get; }
        private IWorkItemBl AzureDevOpsWorkItemBl { get; }

        public MigrateBl(IMilestoneBl milestoneBl, IIterationBl iterationBl, IEpicBl epicBl, IIssueBl issueBl, IWorkItemBl azureDevOpsWorkItemBl)
        {
            MilestoneBl = milestoneBl;
            IterationBl = iterationBl;
            EpicBl = epicBl;
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

            var tickets = new List<Ticket>();

            var epicTickets = await EpicBl.Get();

            if (epicTickets != null)
            {
                tickets.AddRange(epicTickets);
            }

            var issueTickets = await IssueBl.Get();

            if (issueTickets != null)
            {
                tickets.AddRange(issueTickets);
            }

            await AzureDevOpsWorkItemBl.CreateWorkItems(cycles, tickets);
        }
    }
}