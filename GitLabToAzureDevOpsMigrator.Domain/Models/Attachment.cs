using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models;

public class Attachment
{
    public Attachment(string name, string urlPath, string urlPathCleaned, AttachmentReference? azureDevOpAttachmentReference)
    {
        Name = name;
        UrlPath = urlPath;
        UrlPathCleaned = urlPathCleaned;
        AzureDevOpAttachmentReference = azureDevOpAttachmentReference;
    }

    public string Name { get; set; }
    public string UrlPath { get; set; }
    public string UrlPathCleaned { get; set; }
    public AttachmentReference? AzureDevOpAttachmentReference { get; set; }
}