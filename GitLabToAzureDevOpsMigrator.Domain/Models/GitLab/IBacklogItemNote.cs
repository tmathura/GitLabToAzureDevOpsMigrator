using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models.GitLab
{
    public interface IBacklogItemNote
    {
        DateTime CreatedAt { get; }
        string Body { get; }
    }
}
