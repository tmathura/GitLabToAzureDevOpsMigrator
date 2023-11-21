using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Interfaces
{
    public interface IBacklogItemNote
    {
        DateTime CreatedAt { get; }
        string CreatedBy { get; }
        string Body { get; }
    }
}
