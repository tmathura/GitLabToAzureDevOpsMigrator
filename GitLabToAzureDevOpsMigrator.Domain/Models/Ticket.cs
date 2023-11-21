using GitLabToAzureDevOpsMigrator.Domain.Interfaces;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GitLabToAzureDevOpsMigrator.Domain.Models
{
    public class Ticket
    {
        public Ticket(IBacklogItem backlogItem, WorkItem? workItem, List<Annotation> annotations)
        {
            BacklogItem = backlogItem;
            WorkItem = workItem;
            Annotations = annotations;
        }

        public IBacklogItem BacklogItem { get; set; }
        public WorkItem? WorkItem { get; set; }
        public List<Annotation> Annotations { get; set; }
    }
}