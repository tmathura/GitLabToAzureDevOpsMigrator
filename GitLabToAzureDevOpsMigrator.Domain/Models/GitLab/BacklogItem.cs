using NGitLab.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models.GitLab
{
    public class BacklogItem<T> : IBacklogItem
    {
        public T Item { get; }

        public BacklogItem(T item)
        {
            if (item is not Epic && item is not Issue)
            {
                throw new ArgumentException("Item must be an Epic or Issue.");
            }

            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            Item = item;
        }

        public DateTime CreatedAt
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.CreatedAt,
                    Epic epic => epic.CreatedAt,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public int Id
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.IssueId,
                    Epic epic => epic.EpicIid,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public string[] Labels
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.Labels,
                    Epic epic => epic.Labels,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public string State
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.State,
                    Epic epic => epic.State.ToString(),
                    _ => throw new NotImplementedException()
                };
            }
        }

        public string Title
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.Title,
                    Epic epic => epic.Title,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public string Description
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.Description,
                    Epic epic => epic.Description,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public string Milestone
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.Milestone?.Title ?? string.Empty,
                    Epic => string.Empty,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public int Weight
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.Weight ?? 0,
                    Epic => 0,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public DateTime ClosedAt
        {
            get
            {
                return Item switch
                {
                    Issue issue => issue.ClosedAt,
                    Epic => DateTime.Now,
                    _ => throw new NotImplementedException()
                };
            }
        }

        public List<Attachment> Attachments { get; set; } = new();
        public List<Issue> RelatedIssues { get; set; } = new();
    }
}
