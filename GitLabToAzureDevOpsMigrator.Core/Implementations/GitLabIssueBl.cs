using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using log4net;
using Microsoft.Extensions.Configuration;
using NGitLab;
using NGitLab.Models;
using System.Text.RegularExpressions;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class GitLabIssueBl : IGitLabIssueBl
    {
        private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
        private IProjectService ProjectService { get; }
        public IGitLabClient GitLabClient { get; }
        public IProjectIssueNoteClient ProjectIssueNoteClient { get; }
        private GitLabSettings GitLabSettings { get; }

        public GitLabIssueBl(IConfiguration configuration, IGitLabClient gitLabClient, IProjectIssueNoteClient projectIssueNoteClient, IProjectService projectService)
        {
            var appSettings = new AppSettings();
            configuration.Bind(appSettings);

            GitLabSettings = appSettings.GitLab;
            GitLabClient = gitLabClient;
            ProjectIssueNoteClient = projectIssueNoteClient;
            ProjectService = projectService;
        }
        
        public async Task<List<FullIssueDetails>?> CollectIssues()
        {
            var statisticsRoot = await ProjectService.GetIssuesStatistics(GitLabSettings.ProjectId, new List<string> { "team::Core" });

            if (statisticsRoot == null)
            {
                const string noStatisticsMessage = "Collecting GitLab issues encountered a problem, StatisticsRoot is null.";

                Console.WriteLine(noStatisticsMessage);
                Logger.Info(noStatisticsMessage);

                return null;
            }

            var allIssuesCount = statisticsRoot.Statistics.Counts.All;

            var startingProcessMessage = $"Started collecting GitLab issues, there are {allIssuesCount} issues to collect.";

            Console.WriteLine(startingProcessMessage);
            Logger.Info(startingProcessMessage);

            var project = await GitLabClient.Projects.GetByIdAsync(GitLabSettings.ProjectId, new SingleProjectQuery());

            var issueWebUri = new Uri(project.WebUrl);
            var projectUrl = $"{issueWebUri.Scheme}://{issueWebUri.Host}";
            var projectUrlSegments = $"/{issueWebUri.Segments[1]}{issueWebUri.Segments[2]}".TrimEnd('/');

            var issues = GitLabClient.Issues.GetAsync(GitLabSettings.ProjectId, new IssueQuery { Labels = "team::Core" });

            var count = 0;
            var fullIssueDetailsList = new List<FullIssueDetails>();

            var semaphore = new SemaphoreSlim(10); // Set the maximum number of parallel tasks
            var tasks = new List<Task<ProcessIssueResult>>();

            await foreach (var issue in issues)
            {
                count++;

                await semaphore.WaitAsync(); // Wait until the semaphore is available

                tasks.Add(ProcessIssueAsync(issue, projectUrl, projectUrlSegments, fullIssueDetailsList, count, allIssuesCount, semaphore));
            }

            var processedResults = await Task.WhenAll(tasks);

            var processedCount = processedResults.Sum(result => result.Count);
            var errorCount = processedResults.Sum(result => result.ErrorCount);

            var endingProcessMessage = $"{Environment.NewLine}Finished collecting GitLab issues, there are {processedCount} issues collected & there were errors collecting {errorCount} issues.";

            Console.WriteLine(endingProcessMessage);
            Logger.Info(endingProcessMessage);

            return fullIssueDetailsList;
        }

        private async Task<ProcessIssueResult> ProcessIssueAsync(Issue issue, string projectUrl, string projectUrlSegments, ICollection<FullIssueDetails>? fullIssueDetailsCollection, int count, int allIssuesCount, SemaphoreSlim semaphore)
        {
            var processIssueResult = new ProcessIssueResult();

            try
            {
                var fullIssueDetails = new FullIssueDetails(issue, new List<Attachment>(), new List<ProjectIssueNote>(), new List<Attachment>(), new List<Issue>());

                if (!string.IsNullOrWhiteSpace(issue.Description))
                {
                    GetAttachmentInString(issue.Description, projectUrl, projectUrlSegments, fullIssueDetails.IssueAttachments);
                }

                var notes = ProjectIssueNoteClient.ForIssue(issue.IssueId);

                foreach (var note in notes)
                {
                    fullIssueDetails.Notes.Add(note);

                    if (!string.IsNullOrWhiteSpace(note.Body))
                    {
                        GetAttachmentInString(note.Body, projectUrl, projectUrlSegments, fullIssueDetails.NotesAttachments);
                    }
                }

                var relatedIssues = GitLabClient.Issues.LinkedToAsync(GitLabSettings.ProjectId, issue.IssueId);

                await foreach (var relatedIssue in relatedIssues)
                {
                    fullIssueDetails.RelatedIssues.Add(relatedIssue);
                }
                
                fullIssueDetailsCollection.Add(fullIssueDetails);

                DrawConsoleProgressBar(count, allIssuesCount);

                Logger.Info($"Collected {count} GitLab issues so far, issue #{issue.IssueId} - '{issue.Title}' was just collected. ");

                processIssueResult.Count = 1;
            }
            catch (Exception exception)
            {
                Logger.Error($"Error collecting GitLab issue #{issue.IssueId} - '{issue.Title}', was on issue count: {count}.", exception);

                processIssueResult.ErrorCount = 1;
            }
            finally
            {
                semaphore.Release(); // Release the semaphore when the processing is complete
            }

            return processIssueResult;
        }

        private static void GetAttachmentInString(string stringToExtractAttachment, string projectUrl, string projectUrlSegments, ICollection<Attachment> attachments)
        {
            const string attachmentPattern = @"\[([^\]]*)\]\(([^)]*)\)";

            // Use Regex.Match to find all matches in the issue description
            var matches = Regex.Matches(stringToExtractAttachment, attachmentPattern);

            // Iterate through the matches and extract the content inside brackets
            foreach (var match in matches.Cast<Match>())
            {
                var urlPath = match.Groups[2].Value;
                var attachment = new Attachment(match.Groups[1].Value, urlPath, GetAttachmentUri(projectUrl, projectUrlSegments, urlPath));

                attachments.Add(attachment);
            }
        }

        private static void DrawConsoleProgressBar(int progress, int total)
        {
            Console.CursorLeft = 0;

            const int progressBarLength = 100;
            var filledLength = (int)Math.Floor((double)progress / total * progressBarLength);

            Console.Write("[");
            Console.BackgroundColor = ConsoleColor.Green;
            Console.Write(new string(' ', filledLength));
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(new string(' ', progressBarLength - filledLength));
            Console.Write($"] {progress * 100 / total}%");
        }

        private static Uri GetAttachmentUri(string projectUrl, string projectUrlSegments, string attachmentUrlPath)
        {
            var attachmentUrl = attachmentUrlPath.Contains(projectUrlSegments) ? $"{projectUrl}{attachmentUrlPath}" : $"{projectUrl}{projectUrlSegments}{attachmentUrlPath}";

            return new Uri(attachmentUrl);
        }
    }
}