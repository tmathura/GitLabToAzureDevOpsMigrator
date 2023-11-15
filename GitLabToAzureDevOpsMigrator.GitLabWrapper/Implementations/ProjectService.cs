using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab.IssuesStatistics;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using RestSharp;
using System.Net;

namespace GitLabToAzureDevOpsMigrator.GitLabWrapper.Implementations
{
    public class ProjectService : IProjectService
    {
        private IRestClient RestSharpClient { get; }
        public ProjectService(IRestClient restSharpClient)
        {
            RestSharpClient = restSharpClient;
        }
        public async Task<StatisticsRoot?> GetIssuesStatistics(int projectId, List<string> labels)
        {
            var url = $"projects/{projectId}/issues_statistics";

            if (labels.Count > 0)
            {
                url += $"?labels={string.Join(",", labels)}";
            }

            var request = new RestRequest(url);
            var response = await RestSharpClient.ExecuteAsync<StatisticsRoot>(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Http status code is: {response.StatusCode}. {response.Content}");
            }

            return response.Data;
        }
    }
}