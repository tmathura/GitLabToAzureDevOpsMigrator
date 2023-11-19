using GitLabToAzureDevOpsMigrator.Domain.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;

public interface IEpicBl
{
    Task<List<Ticket>?> Get();
}