using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models
{
    public class CommentNote
    {
        public CommentNote(ProjectIssueNote note, List<Attachment> notesAttachments, Comment? comment)
        {
            Note = note;
            NotesAttachments = notesAttachments;
            Comment = comment;
        }

        public ProjectIssueNote Note { get; set; }
        public List<Attachment> NotesAttachments { get; set; }
        public Comment? Comment { get; set; }
    }
}
