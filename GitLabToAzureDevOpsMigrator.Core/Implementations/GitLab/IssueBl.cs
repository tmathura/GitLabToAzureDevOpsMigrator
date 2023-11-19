﻿using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Core.Interfaces.GitLab;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.Settings;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using log4net;
using Microsoft.Extensions.Configuration;
using NGitLab;
using NGitLab.Models;
using System.Text.RegularExpressions;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations.GitLab
{
    public class IssueBl : IIssueBl
    {
        private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
        private IConsoleHelper ConsoleHelper { get; }
        public IProjectIssueNoteClient ProjectIssueNoteClient { get; }
        public IIssueClient IssueClient { get; }
        private IProjectService ProjectService { get; }
        private GitLabSettings GitLabSettings { get; }

        public IssueBl(IConfiguration configuration, IConsoleHelper consoleHelper, IProjectIssueNoteClient projectIssueNoteClient, IIssueClient issueClient, IProjectService projectService)
        {
            var appSettings = new AppSettings();
            configuration.Bind(appSettings);

            GitLabSettings = appSettings.GitLab;
            ConsoleHelper = consoleHelper;
            ProjectIssueNoteClient = projectIssueNoteClient;
            IssueClient = issueClient;
            ProjectService = projectService;
        }

        public async Task<List<Ticket>?> Get()
        {
            var statisticsRoot = await ProjectService.GetIssuesStatistics(GitLabSettings.ProjectId, GitLabSettings.LabelToMigrate);

            if (statisticsRoot == null)
            {
                var noStatisticsMessage = $"{Environment.NewLine}Getting GitLab issues encountered a problem, StatisticsRoot is null.";

                Console.WriteLine(noStatisticsMessage);
                Logger.Info(noStatisticsMessage);

                return null;
            }

            var allIssuesCount = statisticsRoot.Statistics.Counts.All;

            var startingProcessMessage = $"{Environment.NewLine}Started getting GitLab issues, there are {allIssuesCount} issues to retrieve.";

            Console.WriteLine(startingProcessMessage);
            Logger.Info(startingProcessMessage);

            var projectUrlSegments = $"/{GitLabSettings.GroupName}/{GitLabSettings.ProjectName}";

            var issues = IssueClient.GetAsync(GitLabSettings.ProjectId, new IssueQuery { Labels = GitLabSettings.LabelToMigrate });

            var count = 0;
            var tickets = new List<Ticket>();

            var semaphore = new SemaphoreSlim(10); // Set the maximum number of parallel tasks
            var tasks = new List<Task<ProcessIssueResult>>();

            await foreach (var issue in issues)
            {
                count++;

                await semaphore.WaitAsync(); // Wait until the semaphore is available

                tasks.Add(ProcessIssueAsync(issue, projectUrlSegments, tickets, count, allIssuesCount, semaphore));
            }

            var processedResults = await Task.WhenAll(tasks);

            var processedCount = processedResults.Sum(result => result.Count);
            var errorCount = processedResults.Sum(result => result.ErrorCount);

            var endingProcessMessage = $"{Environment.NewLine}Finished getting GitLab issues, there were {processedCount} issues retrieved & there were errors getting {errorCount} issues.";

            Console.WriteLine(endingProcessMessage);
            Logger.Info(endingProcessMessage);

            return tickets;
        }

        private async Task<ProcessIssueResult> ProcessIssueAsync(Issue issue, string projectUrlSegments, ICollection<Ticket> tickets, int count, int allIssuesCount, SemaphoreSlim semaphore)
        {
            var processIssueResult = new ProcessIssueResult();

            try
            {
                var ticket = new Ticket(issue, null, new List<Attachment>(), new List<CommentNote>(), new List<Issue>());

                if (!string.IsNullOrWhiteSpace(issue.Description))
                {
                    GetAttachmentInString(issue.Description, projectUrlSegments, ticket.IssueAttachments);
                }

                var notes = ProjectIssueNoteClient.ForIssue(issue.IssueId);

                foreach (var note in notes)
                {
                    var commentNote = new CommentNote(note, new List<Attachment>(), null);

                    ticket.CommentNotes.Add(commentNote);

                    if (!string.IsNullOrWhiteSpace(note.Body))
                    {
                        GetAttachmentInString(note.Body, projectUrlSegments, commentNote.NotesAttachments);
                    }
                }

                var relatedIssues = IssueClient.LinkedToAsync(GitLabSettings.ProjectId, issue.IssueId);

                await foreach (var relatedIssue in relatedIssues)
                {
                    ticket.RelatedIssues.Add(relatedIssue);
                }

                tickets.Add(ticket);

                ConsoleHelper.DrawConsoleProgressBar(count, allIssuesCount);

                Logger.Info($"Retrieved {count} GitLab issues so far, issue #{issue.IssueId} - '{issue.Title}' was just retrieved. ");

                processIssueResult.Count = 1;
            }
            catch (Exception exception)
            {
                Logger.Error($"Error getting GitLab issue #{issue.IssueId} - '{issue.Title}', was on issue count: {count}.", exception);

                processIssueResult.ErrorCount = 1;
            }
            finally
            {
                semaphore.Release(); // Release the semaphore when the processing is complete
            }

            return processIssueResult;
        }

        private static void GetAttachmentInString(string stringToExtractAttachment, string projectUrlSegments, ICollection<Attachment> attachments)
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

                var attachment = new Attachment(match.Groups[1].Value, urlPath, urlPath.Replace(projectUrlSegments, string.Empty), null);

                attachments.Add(attachment);
            }
        }
    }
}