using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models
{
    public class Ticket
    {
        public Ticket(Issue issue, WorkItem? workItem, List<Attachment> issueAttachments, List<CommentNote> commentNotes, List<Issue> relatedIssues)
        {
            Issue = issue;
            WorkItem = workItem;
            IssueAttachments = issueAttachments;
            CommentNotes = commentNotes;
            RelatedIssues = relatedIssues;
        }

        public Issue Issue { get; set; }
        public WorkItem? WorkItem { get; set; }
        public List<Attachment> IssueAttachments { get; set; }
        public List<CommentNote> CommentNotes { get; set; }
        public List<Issue> RelatedIssues { get; set; }
    }
}