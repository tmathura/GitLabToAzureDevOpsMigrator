using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
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

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.AzureDevOps
{
    public class WorkItemBl : IWorkItemBl
    {
        private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
        private IConsoleHelper ConsoleHelper { get; }
        private IRestClient RestSharpClient { get; }
        private AppSettings AppSettings { get; } = new();
        private WorkItemTrackingHttpClient WorkItemTrackingHttpClient { get; }

        public WorkItemBl(IConfiguration configuration, IConsoleHelper consoleHelper, IVssConnection vssConnection, IRestClient restSharpClient)
        {
            configuration.Bind(AppSettings);

            ConsoleHelper = consoleHelper;
            RestSharpClient = restSharpClient;

            var workItemTrackingHttpClient = vssConnection.GetClient<WorkItemTrackingHttpClient>();

            WorkItemTrackingHttpClient = workItemTrackingHttpClient ?? throw new Exception("WorkItemTrackingHttpClient is null.");
        }

        public async Task<List<WorkItem>> GetAllWorkItems()
        {
            // create a wiql object and build our query to get all work items ids from the project
            var wiql = new Wiql
            {
                Query = "Select [Id] " +
                        "From WorkItems " +
                        $"Where [System.TeamProject] = '{AppSettings.AzureDevOps.ProjectName}' "
            };

            var result = await WorkItemTrackingHttpClient.QueryByWiqlAsync(wiql);
            var ids = result.WorkItems.Select(item => item.Id).ToArray();

            var workItems = await WorkItemTrackingHttpClient.GetWorkItemsAsync(AppSettings.AzureDevOps.ProjectName, ids);

            return workItems;
        }

        public async Task<List<Ticket>?> CreateWorkItems(List<Cycle>? cycles, List<Ticket>? tickets)
        {
            var count = 0;
            var errorCount = 0;

            if (tickets == null || tickets.Count == 0)
            {
                var noTicketsMessage = $"{Environment.NewLine}Creating Azure DevOps work items encountered a problem, no issues to create from.";

                Console.WriteLine(noTicketsMessage);
                Logger.Info(noTicketsMessage);

                return null;
            }

            var startingProcessMessage = $"{Environment.NewLine}Started creating Azure DevOps work items, there are {tickets.Count} issues to create from.";

            Console.WriteLine(startingProcessMessage);
            Logger.Info(startingProcessMessage);

            var workItemsAdded = new Dictionary<int, WorkItem>();

            foreach (var ticket in tickets.OrderBy(ticket => ticket.Issue.CreatedAt))
            {
                try
                {
                    count++;

                    foreach (var attachment in ticket.IssueAttachments)
                    {
                        await UploadAttachment(attachment, ticket.Issue.IssueId);
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

                    var state = ticket.Issue.State switch
                    {
                        "opened" => "New",
                        "closed" => "Closed",
                        "reopened" => "Active",
                        _ => "New"
                    };

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
                        },
                        new()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.State",
                            Value = "New"
                        },
                        new()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.Tags",
                            Value = ticket.Issue.Labels.Any() ? string.Join(";", ticket.Issue.Labels) : null
                        },
                        //new()
                        //{
                        //    Operation = Operation.Add,
                        //    Path = "/fields/System.CreatedBy",
                        //    Value = ticket.Issue.Author.Name
                        //},
                        new()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.CreatedDate",
                            Value = ticket.Issue.CreatedAt
                        }
                    };

                    if (!string.IsNullOrWhiteSpace(ticket.Issue.Milestone?.Title))
                    {
                        var iterationName = AppSettings.AzureDevOps.DefaultIterationPath;
                        var cycle = cycles?.FirstOrDefault(x => x.Milestone.Title == ticket.Issue.Milestone?.Title);

                        if (cycle?.Iteration != null)
                        {
                            iterationName = @$"{iterationName}\{cycle.Iteration.Name}";
                        }

                        jsonPatchDocument.Add(new JsonPatchOperation
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.IterationPath",
                            Value = iterationName
                        });
                    }

                    if (ticket.Issue.Weight is > 0)
                    {
                        jsonPatchDocument.Add(new JsonPatchOperation
                        {
                            Operation = Operation.Add,
                            Path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints",
                            Value = ticket.Issue.Weight
                        });
                    }

                    foreach (var relatedIssue in ticket.RelatedIssues)
                    {
                        if (workItemsAdded.TryGetValue(relatedIssue.IssueId, out var relatedWorkItem))
                        {
                            jsonPatchDocument.Add(new JsonPatchOperation
                            {
                                Operation = Operation.Add,
                                Path = "/relations/-",
                                Value = new
                                {
                                    Rel = "System.LinkTypes.Related",
                                    relatedWorkItem.Url
                                }
                            });
                        }
                    }

                    var workItem = await WorkItemTrackingHttpClient.CreateWorkItemAsync(jsonPatchDocument, AppSettings.AzureDevOps.ProjectName, type);

                    if (workItem.Id == null)
                    {
                        continue;
                    }

                    // Update the state of the newly created work item if it is not new
                    if (state != "New")
                    {
                        jsonPatchDocument = new JsonPatchDocument
                        {
                            new()
                            {
                                Operation = Operation.Add,
                                Path = "/fields/System.State",
                                Value = state
                            }
                        };

                        if (state == "Closed")
                        {
                            //jsonPatchDocument.Add(new JsonPatchOperation
                            //{
                            //    Operation = Operation.Add,
                            //    Path = "/fields/Microsoft.VSTS.Common.ClosedBy",
                            //    Value = ticket.Issue.ClosedBy.Name
                            //});

                            jsonPatchDocument.Add(new JsonPatchOperation
                            {
                                Operation = Operation.Add,
                                Path = "/fields/Microsoft.VSTS.Common.ClosedDate",
                                Value = ticket.Issue.ClosedAt
                            });
                        }

                        try
                        {
                            await WorkItemTrackingHttpClient.UpdateWorkItemAsync(jsonPatchDocument, AppSettings.AzureDevOps.ProjectName, workItem.Id.Value);
                        }
                        catch (Exception exception)
                        {
                            Logger.Error($"Error updating Azure DevOps work item state for work item #{workItem.Id.Value}.", exception);
                        }
                    }

                    ticket.WorkItem = workItem;

                    workItemsAdded.Add(ticket.Issue.IssueId, workItem);

                    foreach (var commentNote in ticket.CommentNotes.OrderBy(x => x.Note.CreatedAt))
                    {

                        foreach (var attachment in commentNote.NotesAttachments)
                        {
                            await UploadAttachment(attachment, ticket.Issue.IssueId);
                        }

                        try
                        {
                            var commentCreate = new CommentCreate
                            {
                                Text = ConvertTextToHtmlAndUpdateAttachmentLinks(commentNote.Note.Body, commentNote.NotesAttachments)
                            };

                            var comment = await WorkItemTrackingHttpClient.AddCommentAsync(commentCreate, AppSettings.AzureDevOps.ProjectName, workItem.Id.Value);

                            commentNote.Comment = comment;
                        }
                        catch (Exception exception)
                        {
                            Logger.Error($"Error adding Azure DevOps work item comment for issue #{ticket.Issue.IssueId}.", exception);
                        }
                    }

                    Logger.Info($"Created {count} Azure DevOp work items so far, work item #{workItem.Id} - '{ticket.Issue.Title}' was just created. ");

                    ConsoleHelper.DrawConsoleProgressBar(count, tickets.Count);
                }
                catch (Exception exception)
                {

                    Logger.Error($"Error creating Azure DevOps work item for issue #{ticket.Issue.IssueId} - '{ticket.Issue.Title}', was on issue count: {count}.", exception);

                    errorCount++;
                    count--;

                    ConsoleHelper.DrawConsoleProgressBar(count, tickets.Count);
                }
            }

            var endingProcessMessage = $"{Environment.NewLine}Finished creating Azure DevOps work items, there are {count} work items created & there were errors creating {errorCount} work items.";

            Console.WriteLine(endingProcessMessage);
            Logger.Info(endingProcessMessage);

            return tickets;
        }

        private async Task UploadAttachment(Attachment attachment, int issueId)
        {
            try
            {
                var stream = await GetAttachmentStream(attachment.UrlPathCleaned);

                var attachmentReference = await WorkItemTrackingHttpClient.CreateAttachmentAsync(stream, AppSettings.AzureDevOps.ProjectName, fileName: attachment.Name);

                attachment.AzureDevOpAttachmentReference = attachmentReference;
            }
            catch (Exception exception)
            {
                Logger.Error($"Error uploading Azure DevOps work item attachment for issue #{issueId}.", exception);
            }
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
            var regex = new Regex(@"(?<!\()\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // Replace URLs with Markdown links
            var formattedDescription = regex.Replace(description, "[$0]($0)");

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