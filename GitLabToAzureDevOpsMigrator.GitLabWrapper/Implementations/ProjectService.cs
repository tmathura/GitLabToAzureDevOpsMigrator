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
        public async Task<StatisticsRoot?> GetIssuesStatistics(int projectId, string label)
        {
            var url = $"api/v4/projects/{projectId}/issues_statistics";

            if (!string.IsNullOrWhiteSpace(label))
            {
                url += $"?labels={label}";
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