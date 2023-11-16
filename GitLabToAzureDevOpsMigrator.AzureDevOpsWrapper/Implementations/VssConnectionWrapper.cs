using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Implementations
{
    public class VssConnectionWrapper : VssConnection, IVssConnection
    {
        public VssConnectionWrapper(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {
        }
    }
}
