using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab.IssuesStatistics;

namespace GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;

public interface IProjectService
{
    Task<StatisticsRoot?> GetIssuesStatistics(int projectId, string label);
}