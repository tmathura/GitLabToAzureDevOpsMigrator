using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models.GitLab
{
    public class FullIssueDetails
    {
        public FullIssueDetails(Issue issue, List<Attachment> issueAttachments, List<ProjectIssueNote> notes, List<Attachment> notesAttachments, List<Issue> relatedIssues)
        {
            Issue = issue;
            IssueAttachments = issueAttachments;
            Notes = notes;
            NotesAttachments = notesAttachments;
            RelatedIssues = relatedIssues;
        }

        public Issue Issue { get; set; }
        public List<Attachment> IssueAttachments { get; set; }
        public List<ProjectIssueNote> Notes { get; set; }
        public List<Attachment> NotesAttachments { get; set; }
        public List<Issue> RelatedIssues { get; set; }
    }
}