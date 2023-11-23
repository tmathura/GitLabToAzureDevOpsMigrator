using Newtonsoft.Json;

namespace GitLabToAzureDevOpsMigrator.Domain.Models.EpicNote;

public class Note
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("type")]
    public object Type { get; set; }

    [JsonProperty("body")]
    public string Body { get; set; }

    [JsonProperty("attachment")]
    public object Attachment { get; set; }

    [JsonProperty("author")]
    public Author Author { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("system")]
    public bool System { get; set; }

    [JsonProperty("noteable_id")]
    public int NoteableId { get; set; }

    [JsonProperty("noteable_type")]
    public string NoteableType { get; set; }

    [JsonProperty("project_id")]
    public object ProjectId { get; set; }

    [JsonProperty("resolvable")]
    public bool Resolvable { get; set; }

    [JsonProperty("confidential")]
    public bool Confidential { get; set; }

    [JsonProperty("internal")]
    public bool Internal { get; set; }

    [JsonProperty("noteable_iid")]
    public object NoteableIid { get; set; }

    [JsonProperty("commands_changes")]
    public object CommandsChanges { get; set; }
}