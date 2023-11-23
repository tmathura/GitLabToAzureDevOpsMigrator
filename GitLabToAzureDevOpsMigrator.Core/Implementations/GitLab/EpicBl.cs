using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using log4net;
using NGitLab;
using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.GitLab;

public class EpicBl : IEpicBl
{
    private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IConsoleHelper ConsoleHelper { get; }
    private IEpicClient EpicClient { get; }
    public IGroupService GroupService { get; }

    public EpicBl(IConsoleHelper consoleHelper, IEpicClient epicClient, IGroupService groupService)
    {
        ConsoleHelper = consoleHelper;
        EpicClient = epicClient;
        GroupService = groupService;
    }

    public async Task<List<Ticket>?> Get(int groupId, string groupName, string labelToMigrate)
    {
        const string startingProcessMessage = "Started getting GitLab epics.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        try
        {
            var epics = EpicClient.Get(groupId, new EpicQuery { Labels = labelToMigrate });

            var count = 0;
            var errorCount = 0;
            var tickets = new List<Ticket>();
            var projectUrlSegments = $"/{groupName}";

            foreach (var epic in epics)
            {
                try
                {
                    count++;

                    var ticket = new Ticket(new BacklogItem<Epic>(epic), null, new List<Annotation>());

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(epic.Description))
                        {
                            AttachmentHelper.GetAttachmentInString(epic.Description, projectUrlSegments, ticket.BacklogItem.DescriptionAttachments);
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Error($"Error getting GitLab epic #{epic.EpicIid} - '{epic.Title}' attachment.", exception);
                    }

                    await GetNotes(groupId, epic, projectUrlSegments, ticket);

                    await GetRelatedIssues(groupId, epic, ticket);

                    tickets.Add(ticket);

                    ConsoleHelper.DrawConsoleProgressCount(count);
                }
                catch (Exception exception)
                {
                    errorCount++;

                    Logger.Error($"Error getting GitLab epic #{epic.Id} - '{epic.Title}', was on epic count: {count}.", exception);
                }
            }

            var endingProcessMessage = $"Finished getting GitLab epics, there were {tickets.Count} epics retrieved & there were errors getting {errorCount} epics.";

            Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
            Logger.Info(endingProcessMessage);

            return tickets;
        }
        catch (Exception exception)
        {
            Logger.Error($"Getting GitLab epics encountered a problem: {exception.Message}", exception);

            return null;
        }
    }

    private async Task GetNotes(int groupId, Epic epic, string projectUrlSegments, Ticket ticket)
    {
        try
        {
            var notes = await GroupService.GetEpicNotes(groupId, epic.Id);

            if (notes == null)
            {
                return;
            }

            foreach (var note in notes)
            {
                try
                {
                    var annotation = new Annotation(new BacklogItemNote<Domain.Models.EpicNote.Note>(note), new List<Attachment>(), null);

                    ticket.Annotations.Add(annotation);

                    if (string.IsNullOrWhiteSpace(note.Body))
                    {
                        continue;
                    }

                    try
                    {
                        AttachmentHelper.GetAttachmentInString(note.Body, projectUrlSegments, annotation.NotesAttachments);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error($"Error getting GitLab epic #{epic.EpicIid} - '{epic.Title}' note #{note.Id} attachment.", exception);
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error($"Error getting GitLab epic #{epic.EpicIid} - '{epic.Title}' note #{note.Id}.", exception);
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Error($"Error getting GitLab epic #{epic.EpicIid} - '{epic.Title}' notes.", exception);
        }
    }

    private async Task GetRelatedIssues(int groupId, Epic epic, Ticket ticket)
    {
        try
        {
            var relatedIssues = EpicClient.GetIssuesAsync(groupId, epic.EpicIid);

            await foreach (var relatedIssue in relatedIssues)
            {
                ticket.BacklogItem.RelatedIssues.Add(relatedIssue);
            }
        }
        catch (Exception exception)
        {
            Logger.Error($"Error getting GitLab epic #{epic.EpicIid} - '{epic.Title}' related issues.", exception);
        }
    }
}