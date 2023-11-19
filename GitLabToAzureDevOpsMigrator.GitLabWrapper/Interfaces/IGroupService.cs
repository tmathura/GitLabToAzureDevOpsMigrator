using GitLabToAzureDevOpsMigrator.Domain.Models.EpicNote;

namespace GitLabToAzureDevOpsMigrator.GitLabWrapper.Interfaces;

public interface IGroupService
{

    /// <summary>
    /// Gets all notes for an epic.
    /// </summary>
    /// <param name="groupId">The group id</param>
    /// <param name="epicId">The epic.id not epic.iid</param>
    /// <returns>A list of notes.</returns>
    /// <exception cref="Exception"></exception>
    Task<List<Note>?> GetEpicNotes(int groupId, int epicId);
}