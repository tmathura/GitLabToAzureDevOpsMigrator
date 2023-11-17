using Microsoft.VisualStudio.Services.WebApi;

namespace GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces
{
    /// <summary>
    /// Using decorator pattern to wrap VssConnection so it can be mocked. VssConnection is not mock-able out of the box.
    /// https://stackoverflow.com/questions/74095300/how-to-mock-microsoft-visualstudio-services-webapi-vssconnection
    /// </summary>
    public interface IVssConnection : IDisposable
    {
        public T GetClient<T>() where T : VssHttpClientBase => this.GetClientAsync<T>().SyncResult<T>();
        public Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default(CancellationToken)) where T : VssHttpClientBase;
    }
}