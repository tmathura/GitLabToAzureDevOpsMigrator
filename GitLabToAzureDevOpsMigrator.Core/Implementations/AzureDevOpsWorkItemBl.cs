using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class AzureDevOpsWorkItemBl : IAzureDevOpsWorkItemBl
    {
        private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
        private IVssConnection VssConnection { get; }
        private AzureDevOpsSettings AzureDevOpsSettings { get; }

        public AzureDevOpsWorkItemBl(IConfiguration configuration, IVssConnection vssConnection)
        {
            var appSettings = new AppSettings();
            configuration.Bind(appSettings);

            AzureDevOpsSettings = appSettings.AzureDevOps;
            VssConnection = vssConnection;
        }
        
        public async Task<List<WorkItem>> GetAllWorkItems()
        {
            var workItemTrackingHttpClient = await VssConnection.GetClientAsync<WorkItemTrackingHttpClient>();

            // create a wiql object and build our query to get all work items ids from the project
            var wiql = new Wiql
            {
                Query = "Select [Id] " +
                        "From WorkItems " +
                        $"Where [System.TeamProject] = '{AzureDevOpsSettings.Project}' "
            };

            var result = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql);
            var ids = result.WorkItems.Select(item => item.Id).ToArray();

            var workItems = await workItemTrackingHttpClient.GetWorkItemsAsync(AzureDevOpsSettings.Project, ids);

            return workItems;
        }
    }
}