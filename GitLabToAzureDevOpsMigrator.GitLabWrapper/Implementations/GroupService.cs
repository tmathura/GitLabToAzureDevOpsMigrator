using GitLabToAzureDevOpsMigrator.Domain.Models.EpicNote;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using RestSharp;
using System.Net;

namespace GitLabToAzureDevOpsMigrator.GitLabWrapper.Implementations
{
    public class GroupService : IGroupService
    {
        private IRestClient RestSharpClient { get; }
        public GroupService(IRestClient restSharpClient)
        {
            RestSharpClient = restSharpClient;
        }
        /// <inheritdoc />
        public async Task<List<Note>?> GetEpicNotes(int groupId, int epicId)
        {
            var url = $"api/v4//groups/{groupId}/epics/{epicId}/notes";

            var request = new RestRequest(url);
            var response = await RestSharpClient.ExecuteAsync<List<Note>>(request);
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Http status code is: {response.StatusCode}. {response.Content}");
            }

            return response.Data;
        }
    }
}