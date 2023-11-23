using GitLabToAzureDevOpsMigrator.Domain.Models;
using System.Text.RegularExpressions;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations;

public static class AttachmentHelper
{
    public static void GetAttachmentInString(string stringToExtractAttachment, string projectUrlSegments, ICollection<Attachment> attachments)
    {
        // Create a Regex to find the content inside brackets
        var regex = new Regex(@"\[([^\]]*)\]\(([^)]*)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Find matches
        var matches = regex.Matches(stringToExtractAttachment);

        // Iterate through the matches and extract the content inside brackets
        foreach (var match in matches.Cast<Match>())
        {
            var urlPath = match.Groups[2].Value;

            if (urlPath.Contains("-/issues/"))
            {
                continue;
            }
            // Replace encoding of the attachment URL
            urlPath = urlPath.Replace(@"\_-\", "_-");

            var attachment = new Attachment(match.Groups[1].Value, urlPath, urlPath.Replace(projectUrlSegments, string.Empty), null);

            attachments.Add(attachment);
        }
    }
}