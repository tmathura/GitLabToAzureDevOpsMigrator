using GitLabToAzureDevOpsMigrator.AzureDevOpsWrapper.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.AzureDevOps;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Models.Settings;
using log4net;
using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using NGitLab.Models;
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

        public async Task<WorkItem?> GetWorkItem(int id)
        {
            var workItems = await WorkItemTrackingHttpClient.GetWorkItemsAsync(AppSettings.AzureDevOps.ProjectName, new[] { id });

            return workItems.FirstOrDefault();
        }

        public async Task<List<WorkItem>> GetWorkItems()
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

            return await GetWorkItems(ids);
        }

        private async Task<List<WorkItem>> GetWorkItems(IEnumerable<int> ids)
        {
            var workItems = await WorkItemTrackingHttpClient.GetWorkItemsAsync(AppSettings.AzureDevOps.ProjectName, ids);

            return workItems;
        }

        public async Task<List<Ticket>?> CreateWorkItems(List<Cycle>? cycles, List<Ticket>? tickets)
        {
            var count = 0;
            var errorCount = 0;

            if (tickets == null || tickets.Count == 0)
            {
                var noTicketsMessage = $"{Environment.NewLine}Creating Azure DevOps work items encountered a problem, no GitLab backlog items to create from.";

                Console.WriteLine(noTicketsMessage);
                Logger.Info(noTicketsMessage);

                return null;
            }

            var startingProcessMessage = $"{Environment.NewLine}Started creating Azure DevOps work items, there are {tickets.Count} GitLab backlog items to create from.";

            Console.WriteLine(startingProcessMessage);
            Logger.Info(startingProcessMessage);

            var workItemsAdded = new Dictionary<int, WorkItem>();

            foreach (var ticket in tickets
                         .OrderBy(ticket => ticket.BacklogItem is BacklogItem<Epic> ? nameof(Epic) : nameof(Issue))
                         .ThenBy(ticket => ticket.BacklogItem.CreatedAt))
            {
                var isEpic = ticket.BacklogItem is BacklogItem<Epic>;

                try
                {
                    count++;

                    await UploadDescriptionAttachments(ticket.BacklogItem.Id, ticket.BacklogItem.DescriptionAttachments, isEpic);

                    string type;
                    var descriptionPath = "/fields/System.Description";


                    if (isEpic)
                    {
                        type = "Epic";
                    }
                    else if (ticket.BacklogItem.Labels.Contains("bug"))
                    {
                        type = "Bug";
                        descriptionPath = "/fields/Microsoft.VSTS.TCM.ReproSteps";
                    }
                    else
                    {
                        type = "User Story";
                    }

                    var state = ticket.BacklogItem.State switch
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
                            Value = $"(#{ticket.BacklogItem.Id}) {ticket.BacklogItem.Title}"
                        },new()
                        {
                            Operation = Operation.Add,
                            Path = descriptionPath,
                            Value = ConvertTextToHtmlAndUpdateAttachmentLinks(ticket.BacklogItem.Description, ticket.BacklogItem.DescriptionAttachments)
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
                            Value = ticket.BacklogItem.Labels.Any() ? string.Join(";", ticket.BacklogItem.Labels) : null
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
                            Value = ticket.BacklogItem.CreatedAt
                        }
                    };
                    
                    if (!string.IsNullOrWhiteSpace(ticket.BacklogItem.MilestoneTitle))
                    {
                        var iterationName = AppSettings.AzureDevOps.DefaultIterationPath;
                        var cycle = cycles?.FirstOrDefault(cycle => cycle.Milestone.Title == ticket.BacklogItem.MilestoneTitle);

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

                    if (ticket.BacklogItem.Weight > 0)
                    {
                        jsonPatchDocument.Add(new JsonPatchOperation
                        {
                            Operation = Operation.Add,
                            Path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints",
                            Value = ticket.BacklogItem.Weight
                        });
                    }

                    AddRelatedIssues(ticket.BacklogItem.RelatedIssues, workItemsAdded, jsonPatchDocument);

                    var workItem = await WorkItemTrackingHttpClient.CreateWorkItemAsync(jsonPatchDocument, AppSettings.AzureDevOps.ProjectName, type);

                    if (workItem == null)
                    {
                        throw new Exception("Work item not created from create.");
                    }

                    // Update the state of the newly created work item if it is not new
                    if (state != "New")
                    {
                        await UpdateWorkItem(workItem.Id.Value, state, ticket);
                    }

                    ticket.WorkItem = workItem;

                    workItemsAdded.Add(ticket.BacklogItem.Id, workItem);

                    await AddComments(ticket.BacklogItem.Id, workItem.Id.Value, ticket.Annotations, isEpic);

                    await AddComment(ticket.BacklogItem.Id, workItem.Id.Value, isEpic, $"Work item created from GitLab {GetTicketType(isEpic)} [#{ticket.BacklogItem.Id}]({ticket.BacklogItem.WebUrl})", null);

                    Logger.Info($"Created {count} Azure DevOp work items so far, work item #{workItem.Id} - '{ticket.BacklogItem.Title}' was just created. ");

                    ConsoleHelper.DrawConsoleProgressBar(count, tickets.Count);
                }
                catch (Exception exception)
                {

                    Logger.Error($"Error creating Azure DevOps work item for GitLab {GetTicketType(isEpic)} #{ticket.BacklogItem.Id} - '{ticket.BacklogItem.Title}', was on GitLab backlog item count: {count}.", exception);

                    errorCount++;
                    count--;

                    ConsoleHelper.DrawConsoleProgressBar(count, tickets.Count);
                }
            }

            var endingProcessMessage = $"{Environment.NewLine}Finished creating Azure DevOps work items, there were {count} work items created & there were errors creating {errorCount} work items.";

            Console.WriteLine(endingProcessMessage);
            Logger.Info(endingProcessMessage);

            return tickets;
        }

        private async Task UpdateWorkItem(int workItemId, string state, Ticket ticket)
        {
            var jsonPatchDocument = new JsonPatchDocument
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
                    Value = ticket.BacklogItem.ClosedAt
                });
            }

            try
            {
                await WorkItemTrackingHttpClient.UpdateWorkItemAsync(jsonPatchDocument, AppSettings.AzureDevOps.ProjectName, workItemId);
            }
            catch (Exception exception)
            {
                Logger.Error($"Error updating Azure DevOps work item state for work item #{workItemId}.", exception);
            }
        }

        private async Task UploadDescriptionAttachments(int backlogItemId, List<Attachment> descriptionAttachments, bool isEpic)
        {
            foreach (var attachment in descriptionAttachments)
            {
                await UploadAttachment(backlogItemId, attachment, isEpic);
            }
        }

        private static void AddRelatedIssues(List<Issue> relatedIssues, IReadOnlyDictionary<int, WorkItem> workItemsAdded, JsonPatchDocument jsonPatchDocument)
        {
            foreach (var relatedIssue in relatedIssues)
            {
                if (workItemsAdded.TryGetValue(relatedIssue.Id, out var relatedWorkItem))
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
        }

        private async Task AddComments(int backlogItemId, int workItemId, IEnumerable<Annotation> annotations, bool isEpic)
        {
            foreach (var annotation in annotations.OrderBy(x => x.Note.CreatedAt))
            {
                foreach (var attachment in annotation.NotesAttachments)
                {
                    await UploadAttachment(backlogItemId, attachment, isEpic);
                }

                var comment = await AddComment(backlogItemId, workItemId, isEpic, annotation.Note.Body, annotation.NotesAttachments);

                if (comment != null)
                {
                    annotation.Comment = comment;
                }
            }
        }

        private async Task<Comment?> AddComment(int backlogItemId, int workItemId, bool isEpic, string commentText, List<Attachment>? notesAttachments)
        {
            Comment? comment = null;

            try
            {
                var commentCreate = new CommentCreate
                {
                    Text = ConvertTextToHtmlAndUpdateAttachmentLinks(commentText, notesAttachments)
                };

                comment = await WorkItemTrackingHttpClient.AddCommentAsync(commentCreate, AppSettings.AzureDevOps.ProjectName, workItemId);
            }
            catch (Exception exception)
            {
                Logger.Error($"Error adding Azure DevOps work item comment for GitLab {GetTicketType(isEpic)} #{backlogItemId}.", exception);
            }

            return comment;
        }

        private async Task UploadAttachment(int backlogItemId, Attachment attachment, bool isEpic)
        {
            try
            {
                var stream = await GetAttachmentStream(attachment.UrlPathCleaned, isEpic);

                var attachmentReference = await WorkItemTrackingHttpClient.CreateAttachmentAsync(stream, AppSettings.AzureDevOps.ProjectName, fileName: attachment.Name);

                attachment.AzureDevOpAttachmentReference = attachmentReference;
            }
            catch (Exception exception)
            {
                Logger.Error($"Error uploading Azure DevOps work item attachment for GitLab {GetTicketType(isEpic)} #{backlogItemId}.", exception);
            }
        }

        private async Task<Stream?> GetAttachmentStream(string urlPathCleaned, bool isEpic)
        {
            var attachmentUrl = isEpic ? $"/groups/{AppSettings.GitLab.GroupName}/-{urlPathCleaned}" : $"/{AppSettings.GitLab.GroupName}/{AppSettings.GitLab.ProjectName}{urlPathCleaned}";

            var request = new RestRequest(attachmentUrl);

            // Add the cookie to the request
            var gitLabUri = new Uri(AppSettings.GitLab.Url);
            request.AddCookie("_gitlab_session", AppSettings.GitLab.Cookie, "/", gitLabUri.Host);

            var stream = await RestSharpClient.DownloadStreamAsync(request);
            return stream;
        }

        private static string ConvertTextToHtmlAndUpdateAttachmentLinks(string description, List<Attachment>? attachments)
        {
            // Regular expression pattern for matching URLs
            var regex = new Regex(@"(?<!\()\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // Replace URLs with Markdown links
            var formattedDescription = regex.Replace(description, "[$0]($0)");

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment.AzureDevOpAttachmentReference == null)
                    {
                        continue;
                    }

                    formattedDescription = formattedDescription.Replace(attachment.UrlPath, attachment.AzureDevOpAttachmentReference.Url);
                }
            }

            return Markdown.ToHtml(formattedDescription);
        }

        private static string GetTicketType(bool isEpic)
        {
            return isEpic ? "epic" : "issue";
        }
    }
}