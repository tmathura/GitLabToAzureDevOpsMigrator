using GitLabToAzureDevOpsMigrator.Core.Interfaces;
using GitLabToAzureDevOpsMigrator.Domain.Models;
using GitLabToAzureDevOpsMigrator.Domain.Models.GitLab;
using GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;
using log4net;
using NGitLab;
using NGitLab.Models;
using System.Text.RegularExpressions;

namespace GitLabToAzureDevOpsMigrator.Core.Implementations
{
    public class MigratorBl : IMigratorBl
    {
        private ILog Logger { get; } = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
        private IProjectService ProjectService { get; }

        public MigratorBl(IProjectService projectService)
        {
            ProjectService = projectService;
        }

        public void MigrateEpics(string gitLabUrl, int gitLabGroupId, int gitLabProjectId, string gitLabToken)
        {
            var client = new GitLabClient(gitLabUrl, gitLabToken);

            var epics = client.Epics.Get(gitLabGroupId, new EpicQuery()).ToList();

            Console.WriteLine(epics.Count == 0 ? "No epics found." : $"There are {epics.Count} epics.");

            foreach (var epic in epics)
            {
                Console.WriteLine($"This is epic {epic.EpicIid} - '{epic.Title}'");
            }
        }
        
        public async Task CollectGitLabIssues(string gitLabUrl, int gitLabGroupId, int gitLabProjectId, string gitLabAccessToken)
        {
            var statisticsRoot = await ProjectService.GetIssuesStatistics(gitLabProjectId, new List<string> { "team::Core" });

            if (statisticsRoot == null)
            {
                const string noStatisticsMessage = "Collecting GitLab issues encountered a problem, StatisticsRoot is null.";

                Console.WriteLine(noStatisticsMessage);
                Logger.Info(noStatisticsMessage);

                return;
            }

            var allIssuesCount = statisticsRoot.Statistics.Counts.All;

            var startingProcessMessage = $"Started collecting GitLab issues, there are {allIssuesCount} issues to collect.";

            Console.WriteLine(startingProcessMessage);
            Logger.Info(startingProcessMessage);

            var client = new GitLabClient(gitLabUrl, gitLabAccessToken);
            var project = await client.Projects.GetByIdAsync(gitLabProjectId, new SingleProjectQuery());
            var projectIssueNoteClient = client.GetProjectIssueNoteClient(gitLabProjectId);

            var issueWebUri = new Uri(project.WebUrl);
            var projectUrl = $"{issueWebUri.Scheme}://{issueWebUri.Host}";
            var projectUrlSegments = $"/{issueWebUri.Segments[1]}{issueWebUri.Segments[2]}".TrimEnd('/');

            var issues = client.Issues.GetAsync(gitLabProjectId, new IssueQuery { Labels = "team::Core" });

            var count = 0;
            var gitLabIssues = new List<FullIssueDetails>();

            var semaphore = new SemaphoreSlim(10); // Set the maximum number of parallel tasks
            var tasks = new List<Task<ProcessIssueResult>>();

            await foreach (var issue in issues)
            {
                count++;

                await semaphore.WaitAsync(); // Wait until the semaphore is available

                tasks.Add(ProcessIssueAsync(issue, gitLabProjectId, projectIssueNoteClient, client, projectUrl, projectUrlSegments, gitLabIssues, count, allIssuesCount, semaphore));
            }

            var processedResults = await Task.WhenAll(tasks);

            var processedCount = processedResults.Sum(result => result.Count);
            var errorCount = processedResults.Sum(result => result.ErrorCount);

            var endingProcessMessage = $"{Environment.NewLine}Finished collecting GitLab issues, there are {processedCount} issues collected & there were errors collecting {errorCount} issues.";

            Console.WriteLine(endingProcessMessage);
            Logger.Info(endingProcessMessage);
        }

        private async Task<ProcessIssueResult> ProcessIssueAsync(Issue issue, int gitLabProjectId, IProjectIssueNoteClient projectIssueNoteClient, IGitLabClient client, string projectUrl, string projectUrlSegments, ICollection<FullIssueDetails> gitLabIssues, int count, int allIssuesCount, SemaphoreSlim semaphore)
        {
            var processIssueResult = new ProcessIssueResult();

            try
            {
                var gitLabIssue = new FullIssueDetails(issue, new List<Attachment>(), new List<ProjectIssueNote>(), new List<Attachment>(), new List<Issue>());

                GetAttachmentInString(issue.Description, projectUrl, projectUrlSegments, gitLabIssue.IssueAttachments);

                var notes = projectIssueNoteClient.ForIssue(issue.IssueId);

                foreach (var note in notes)
                {
                    gitLabIssue.Notes.Add(note);

                    GetAttachmentInString(note.Body, projectUrl, projectUrlSegments, gitLabIssue.NotesAttachments);
                }

                var relatedIssues = client.Issues.LinkedToAsync(gitLabProjectId, issue.IssueId);

                await foreach (var relatedIssue in relatedIssues)
                {
                    gitLabIssue.RelatedIssues.Add(relatedIssue);
                }
                
                gitLabIssues.Add(gitLabIssue);

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