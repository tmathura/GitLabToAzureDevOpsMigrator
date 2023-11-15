using Newtonsoft.Json;

namespace GitLabToAzureDevOpsMigrator.Domain.Models.GitLab.IssuesStatistics;

public class Counts
{
    [JsonProperty("all")]
    public int All { get; set; }

    [JsonProperty("closed")]
    public int Closed { get; set; }

    [JsonProperty("opened")]
    public int Opened { get; set; }
}