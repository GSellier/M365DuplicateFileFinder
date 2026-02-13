using M365DuplicateFileFinder.Models;

namespace M365DuplicateFileFinder.Readers;

/// <summary>
/// Represents an interface that can be implemented by classes reading files.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Gets the files.
    /// </summary>
    /// <returns>
    /// A Task object representing the asynchronous operation. The result of the Task is the list of files.
    /// </returns>
    public Task<IEnumerable<M365File>> GetFilesAsync();
}