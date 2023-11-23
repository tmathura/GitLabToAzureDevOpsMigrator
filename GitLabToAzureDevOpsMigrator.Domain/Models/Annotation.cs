using GitLabToAzureDevOpsMigrator.Domain.Interfaces;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models;

public class Annotation
{
    public Annotation(IBacklogItemNote note, List<Attachment> notesAttachments, Comment? comment)
    {
        Note = note;
        NotesAttachments = notesAttachments;
        Comment = comment;
    }

    public IBacklogItemNote Note { get; set; }
    public List<Attachment> NotesAttachments { get; set; }
    public Comment? Comment { get; set; }
}