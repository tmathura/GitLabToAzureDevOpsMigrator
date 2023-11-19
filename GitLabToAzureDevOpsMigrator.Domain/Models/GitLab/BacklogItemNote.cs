using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models.GitLab
{
    public class BacklogItemNote<T> : IBacklogItemNote
    {
        public T Note { get; }

        public BacklogItemNote(T note)
        {
            if (note is not ProjectIssueNote && note is not EpicNote.Note)
            {
                throw new ArgumentException("Note must be an Epic or Issue.");
            }

            if (note is null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            Note = note;
        }

        public DateTime CreatedAt
        {
            get
            {
                return Note switch
                {
                    ProjectIssueNote projectIssueNote => projectIssueNote.CreatedAt,
                    EpicNote.Note epicNote => epicNote.CreatedAt,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public string Body
        {
            get
            {
                return Note switch
                {
                    ProjectIssueNote projectIssueNote => projectIssueNote.Body,
                    EpicNote.Note epicNote => epicNote.Body,
                    _ => throw new NotImplementedException()
                };
            }
        }
    }
}
