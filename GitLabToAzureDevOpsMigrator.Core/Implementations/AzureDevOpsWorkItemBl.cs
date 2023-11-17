using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.Settings;
using log4net;
using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using RestSharp;
using System.Text.RegularExpressions;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class AzureDevOpsWorkItemBl : IAzureDevOpsWorkItemBl
    {
        private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
        private IVssConnection VssConnection { get; }
        private IRestClient RestSharpClient { get; }
        private AppSettings AppSettings { get; } = new();

        public AzureDevOpsWorkItemBl(IConfiguration configuration, IVssConnection vssConnection, IRestClient restSharpClient)
        {
            configuration.Bind(AppSettings);

            VssConnection = vssConnection;
            RestSharpClient = restSharpClient;
        }

        public async Task<List<WorkItem>> GetAllWorkItems()
        {
            var workItemTrackingHttpClient = await VssConnection.GetClientAsync<WorkItemTrackingHttpClient>();

            // create a wiql object and build our query to get all work items ids from the project
            var wiql = new Wiql
            {
                Query = "Select [Id] " +
                        "From WorkItems " +
                        $"Where [System.TeamProject] = '{AppSettings.AzureDevOps.ProjectName}' "
            };

            var result = await workItemTrackingHttpClient.QueryByWiqlAsync(wiql);
            var ids = result.WorkItems.Select(item => item.Id).ToArray();

            var workItems = await workItemTrackingHttpClient.GetWorkItemsAsync(AppSettings.AzureDevOps.ProjectName, ids);

            return workItems;
        }

        public async Task<List<Ticket>> CreateWorkItems(List<Ticket> tickets)
        {
            var count = 0;
            var workItemTrackingHttpClient = await VssConnection.GetClientAsync<WorkItemTrackingHttpClient>();

            foreach (var ticket in tickets)
            {
                count++;

                try
                {
                    foreach (var attachment in ticket.IssueAttachments)
                    {
                        await UploadAttachment(attachment, workItemTrackingHttpClient);
                    }

                    string type;
                    string descriptionPath;

                    if (ticket.Issue.Labels.Contains("bug"))
                    {
                        type = "Bug";
                        descriptionPath = "/fields/Microsoft.VSTS.TCM.ReproSteps";
                    }
                    else
                    {
                        type = "User Story";
                        descriptionPath = "/fields/System.Description";
                    }

                    // Construct the object containing field values required for the new work item
                    var jsonPatchDocument = new JsonPatchDocument
                    {
                        new()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.Title",
                            Value = ticket.Issue.Title
                        },
                        new()
                        {
                            Operation = Operation.Add,
                            Path = descriptionPath,
                            Value = ConvertTextToHtmlAndUpdateAttachmentLinks(ticket.Issue.Description, ticket.IssueAttachments)
                        }
                    };

                    var workItem = await workItemTrackingHttpClient.CreateWorkItemAsync(jsonPatchDocument, AppSettings.AzureDevOps.ProjectName, type);

                    if (workItem.Id == null)
                    {
                        continue;
                    }

                    ticket.WorkItem = workItem;

                    foreach (var commentNote in ticket.CommentNotes.OrderBy(x => x.Note.CreatedAt))
                    {

                        foreach (var attachment in commentNote.NotesAttachments)
                        {
                            await UploadAttachment(attachment, workItemTrackingHttpClient);
                        }

                        var commentCreate = new CommentCreate
                        {
                            Text = ConvertTextToHtmlAndUpdateAttachmentLinks(commentNote.Note.Body, commentNote.NotesAttachments)
                        };

                        var comment = await workItemTrackingHttpClient.AddCommentAsync(commentCreate, AppSettings.AzureDevOps.ProjectName, workItem.Id.Value);

                        commentNote.Comment = comment;
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error($"Error creating Azure DevOps work item for issue #{ticket.Issue.IssueId} - '{ticket.Issue.Title}', was on issue count: {count}.", exception);
                    throw;
                }
            }

            return tickets;
        }

        private async Task UploadAttachment(Attachment attachment, WorkItemTrackingHttpClientBase workItemTrackingHttpClient)
        {
            var stream = await GetAttachmentStream(attachment.UrlPathCleaned);

            var attachmentReference = await workItemTrackingHttpClient.CreateAttachmentAsync(stream, AppSettings.AzureDevOps.ProjectName, fileName: attachment.Name);

            attachment.AzureDevOpAttachmentReference = attachmentReference;
        }

        private async Task<Stream?> GetAttachmentStream(string urlPathCleaned)
        {
            var attachmentUrl = $"/{AppSettings.GitLab.GroupName}/{AppSettings.GitLab.ProjectName}{urlPathCleaned}";
            var request = new RestRequest(attachmentUrl);

            // Add the cookie to the request
            var gitLabUri = new Uri(AppSettings.GitLab.Url);
            request.AddCookie("_gitlab_session", AppSettings.GitLab.Cookie, "/", gitLabUri.Host);

            var stream = await RestSharpClient.DownloadStreamAsync(request);
            return stream;
        }

        private static string ConvertTextToHtmlAndUpdateAttachmentLinks(string description, List<Attachment> attachments)
        {
            // Regular expression pattern for matching URLs
            const string urlPattern = @"(?<url>https?://[^\s]+)";

            // Create a Regex object
            var regex = new Regex(urlPattern);

            // Replace URLs with Markdown links
            var formattedDescription = regex.Replace(description, "[$1]($1)");

            foreach (var attachment in attachments)
            {
                if (attachment.AzureDevOpAttachmentReference == null)
                {
                    continue;
                }

                formattedDescription = formattedDescription.Replace(attachment.UrlPath, attachment.AzureDevOpAttachmentReference.Url);
            }

            return Markdown.ToHtml(formattedDescription);
        }
    }
}