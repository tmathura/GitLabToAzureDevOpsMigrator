using GitLabToAzureDevOpsMigrator.Domain.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface IIterationBl
{
    Task<List<Cycle>?> Create(string projectName, List<Cycle>? cycles);
}