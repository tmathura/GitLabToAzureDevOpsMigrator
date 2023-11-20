using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models.GitLab
{
    public interface IBacklogItem
    {
        DateTime CreatedAt { get; }
        int Id { get; }
        string[] Labels { get; }
        string State { get; }
        string Title { get; }
        string Description { get; }
        string MilestoneTitle { get; }
        int Weight { get; }
        DateTime ClosedAt { get; }
        string WebUrl { get; }
        List<Attachment> DescriptionAttachments { get; }
        List<Issue> RelatedIssues { get; }
    }
}
