using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using log4net;
using NGitLab;
using NGitLab.Models;
using System.Text.RegularExpressions;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.GitLab;

public class IssueBl : IIssueBl
{
    private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IConsoleHelper ConsoleHelper { get; }
    private IProjectIssueNoteClient ProjectIssueNoteClient { get; }
    private IIssueClient IssueClient { get; }
    private IMergeRequestClient MergeRequestClient { get; }
    private IProjectService ProjectService { get; }

    public IssueBl(IConsoleHelper consoleHelper, IProjectIssueNoteClient projectIssueNoteClient, IIssueClient issueClient, IMergeRequestClient mergeRequestClient, IProjectService projectService)
    {
        ConsoleHelper = consoleHelper;
        ProjectIssueNoteClient = projectIssueNoteClient;
        IssueClient = issueClient;
        MergeRequestClient = mergeRequestClient;
        ProjectService = projectService;
    }

    public async Task<List<Ticket>?> Get(string groupName, int projectId, string projectName, string labelToMigrate)
    {
        var statisticsRoot = await ProjectService.GetIssuesStatistics(projectId, labelToMigrate);

        if (statisticsRoot == null)
        {
            const string noStatisticsMessage = "Getting GitLab issues encountered a problem, StatisticsRoot is null.";

            Console.WriteLine($"{Environment.NewLine}{noStatisticsMessage}");
            Logger.Info(noStatisticsMessage);

            return null;
        }

        var allIssuesCount = statisticsRoot.Statistics.Counts.All;

        var startingProcessMessage = $"Started getting GitLab issues, there are {allIssuesCount} issues to retrieve.";

        Console.WriteLine($"{Environment.NewLine}{startingProcessMessage}");
        Logger.Info(startingProcessMessage);

        var projectUrlSegments = $"/{groupName}/{projectName}";

        var issues = IssueClient.GetAsync(projectId, new IssueQuery { Labels = labelToMigrate });

        var count = 0;
        var tickets = new List<Ticket>();

        var semaphore = new SemaphoreSlim(10); // Set the maximum number of parallel tasks
        var tasks = new List<Task<ProcessResult>>();

        await foreach (var issue in issues)
        {
            count++;

            await semaphore.WaitAsync(); // Wait until the semaphore is available

            tasks.Add(ProcessIssueAsync(projectId, issue, projectUrlSegments, tickets, count, allIssuesCount, semaphore));
        }

        var processedResults = await Task.WhenAll(tasks);

        var processedCount = processedResults.Sum(result => result.Count);
        var errorCount = processedResults.Sum(result => result.ErrorCount);

        var endingProcessMessage = $"Finished getting GitLab issues, there were {processedCount} issues retrieved & there were errors getting {errorCount} issues.";

        Console.WriteLine($"{Environment.NewLine}{endingProcessMessage}");
        Logger.Info(endingProcessMessage);

        return tickets;
    }

    private async Task<ProcessResult> ProcessIssueAsync(int projectId, Issue issue, string projectUrlSegments, ICollection<Ticket> tickets, int count, int allIssuesCount, SemaphoreSlim semaphore)
    {
        var processResult = new ProcessResult();

        try
        {
            var ticket = new Ticket(new BacklogItem<Issue>(issue), null, new List<Annotation>());

            try
            {
                if (!string.IsNullOrWhiteSpace(issue.Description))
                {
                    AttachmentHelper.GetAttachmentInString(issue.Description, projectUrlSegments, ticket.BacklogItem.DescriptionAttachments);
                }
            }
            catch (Exception exception)
            {
                Logger.Error($"Error getting GitLab issue #{issue.IssueId} - '{issue.Title}' attachment.", exception);
            }

            await GetNotes(issue, projectUrlSegments, ticket);

            await GetRelatedIssues(projectId, issue, ticket);

            tickets.Add(ticket);

            ConsoleHelper.DrawConsoleProgressBar(allIssuesCount);

            Logger.Info($"Retrieved {count} GitLab issues so far, issue #{issue.IssueId} - '{issue.Title}' was just retrieved.");

            processResult.Count = 1;
        }
        catch (Exception exception)
        {
            Logger.Error($"Error getting GitLab issue #{issue.IssueId} - '{issue.Title}', was on issue count: {count}.", exception);

            processResult.ErrorCount = 1;
        }
        finally
        {
            semaphore.Release(); // Release the semaphore when the processing is complete
        }

        return processResult;
    }

    private async Task GetNotes(Issue issue, string projectUrlSegments, Ticket ticket)
    {
        try
        {
            var notes = ProjectIssueNoteClient.ForIssue(issue.IssueId);

            foreach (var note in notes)
            {
                try
                {
                    if (note.Body.Contains("merge request"))
                    {
                        // Use a regular expression to match numbers at the end of the string
                        var match = Regex.Match(note.Body, @"\d+$");

                        if (match.Success && int.TryParse(match.Value, out var mergeRequestId))
                        {
                            try
                            {
                                var merge = await MergeRequestClient.GetByIidAsync(mergeRequestId, new SingleMergeRequestQuery());

                                ticket.BacklogItem.MergeRequests.Add(merge);
                            }
                            catch (Exception exception)
                            {
                                Logger.Error($"Error getting GitLab issue !{issue.IssueId} - '{issue.Title}' merge request #{mergeRequestId}.", exception);
                            }
                        }
                    }

                    var annotation = new Annotation(new BacklogItemNote<ProjectIssueNote>(note), new List<Attachment>(), null);

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
                        Logger.Error($"Error getting GitLab issue #{issue.IssueId} - '{issue.Title}' note #{note.NoteId} attachment.", exception);
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error($"Error getting GitLab issue #{issue.IssueId} - '{issue.Title}' note #{note.NoteId}.", exception);
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Error($"Error getting GitLab issue #{issue.IssueId} - '{issue.Title}' notes.", exception);
        }
    }

    private async Task GetRelatedIssues(int projectId, Issue issue, Ticket ticket)
    {
        try
        {
            var relatedIssues = IssueClient.LinkedToAsync(projectId, issue.IssueId);

            await foreach (var relatedIssue in relatedIssues)
            {
                ticket.BacklogItem.RelatedIssues.Add(relatedIssue);
            }
        }
        catch (Exception exception)
        {
            Logger.Error($"Error getting GitLab issue #{issue.IssueId} - '{issue.Title}' related issues.", exception);
        }
    }
}