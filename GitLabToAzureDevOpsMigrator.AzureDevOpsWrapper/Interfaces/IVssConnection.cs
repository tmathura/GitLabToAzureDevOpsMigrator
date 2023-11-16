using Microsoft.VisualStudio.Services.WebApi;

namespace GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces
{
    public interface IVssConnection : IDisposable
    {
        public T GetClient<T>() where T : VssHttpClientBase => this.GetClientAsync<T>().SyncResult<T>();
        public Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default(CancellationToken)) where T : VssHttpClientBase;
    }
}