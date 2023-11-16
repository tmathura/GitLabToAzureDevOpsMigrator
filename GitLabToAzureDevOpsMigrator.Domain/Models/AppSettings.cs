using GitLabToAzureDevOpsMigrator.Domain.Models.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab;

namespace GitLabToAzureDevOpsMigrator.Domain.Models
{
    public class AppSettings
    {
        public GitLabSettings GitLab { get; set; }
        public AzureDevOpsSettings AzureDevOps { get; set; }
    }
}
