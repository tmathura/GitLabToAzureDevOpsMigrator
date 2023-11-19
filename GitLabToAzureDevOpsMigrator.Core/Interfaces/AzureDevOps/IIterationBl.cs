using GitLabToAzureDevOpsMigrator.Domain.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;

public interface IIterationBl
{
    Task<List<Cycle>?> Create(List<Cycle>? cycles);
}