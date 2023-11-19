using GitLabToAzureDevOpsMigrator.Domain.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;

public interface IMilestoneBl
{
    List<Cycle>? Get();
}