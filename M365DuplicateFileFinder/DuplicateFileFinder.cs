using M365DuplicateFileFinder.Models;
using M365DuplicateFileFinder.Readers;

namespace M365DuplicateFileFinder;

/// <summary>
/// Provides logic to identify duplicate files.
/// </summary>
public class DuplicateFileFinder
{
    /// <summary>
    /// Multiple hash properties exist, but are not always populated. The code will use those configured in this variable for duplicate detection.
    /// Checking all 3 hash properties publicly provided by the Graph API, in the order they seem to be the most frequently populated.
    /// Using multiple is only useful if the previous ones are empty, or in case different files have an identical hash value.
    /// </summary>
    private static readonly Func<M365File, string>[] s_hashesGroupingKeySelectorsOrdered =
        [
            GetM365FileQuickXorHash,
            GetM365FileSha1Hash,
            GetM365FileCrc32Hash
        ];
    private static readonly string s_emptyHash = string.Empty;

    /// <summary>
    /// An IFileReader that will be used to read files.
    /// </summary>
    public required IFileReader FileReader { get; init; }

    /// <summary>
    /// Initializes a new instance of the DuplicateFileFinder class.
    /// </summary>
    public DuplicateFileFinder()
    {
    }

    /// <summary>
    /// Calls the file reader to get the list of files, and then analyzes them to identify duplicate ones.
    /// </summary>
    /// <returns>
    /// A Task object representing the asynchronous operation. The result of the Task is the list of duplicate file groups.
    /// </returns>
    public async Task<IEnumerable<IEnumerable<M365File>>> GetDuplicateFilesAsync()
    {
        IEnumerable<M365File> files = await FileReader.GetFilesAsync();
        return DuplicateFileFinder.GetDuplicateFiles(files);
    }

    private static List<IEnumerable<M365File>> GetDuplicateFiles(IEnumerable<M365File> files)
    {
        // Grouping files by size first. Size is always populated, hashes are not.
        // Not sure if performance is better with or without this. Keeping it for now.
        Dictionary<long, List<M365File>> filesGroupedBySize = DuplicateFileFinder.GroupFiles<long>(files, GetM365FileSize);

        var filesGroupedByHashes = new List<IEnumerable<M365File>>();
        foreach (IEnumerable<M365File> fileSizeGroup in filesGroupedBySize.Values)
            if (fileSizeGroup.Count() > 1)
            {
                List<IEnumerable<M365File>> fileGroups = DuplicateFileFinder.GroupDuplicateFilesBasedOnHashes(fileSizeGroup);
                foreach (IEnumerable<M365File> fileGroup in fileGroups)
                {
                    filesGroupedByHashes.Add(fileGroup);
                }
            }

        return filesGroupedByHashes;
    }

    private static long GetM365FileSize(M365File file)
    {
        return file.Size;
    }

    private static Dictionary<TGroupingType, List<M365File>> GroupFiles<TGroupingType>(
        IEnumerable<M365File> files,
        Func<M365File, TGroupingType> groupingKeySelector)
            where TGroupingType : notnull
    {
        var fileGroups = new Dictionary<TGroupingType, List<M365File>>();

        foreach (M365File file in files)
        {
            TGroupingType groupingKey = groupingKeySelector(file);

            if (fileGroups.TryGetValue(groupingKey, out List<M365File>? filesWithSameGroupingKey))
            {
                filesWithSameGroupingKey.Add(file);
            }
            else
            {
                fileGroups.Add(groupingKey, [file]);
            }
        }

        return fileGroups;
    }

    private static List<IEnumerable<M365File>> GroupDuplicateFilesBasedOnHashes(IEnumerable<M365File> files)
    {
        List<IEnumerable<M365File>> fileGroups = [files];

        // Grouping files on the combination of all requested hash properties.
        foreach (Func<M365File, string> groupingKeySelector in DuplicateFileFinder.s_hashesGroupingKeySelectorsOrdered)
        {
            var filesGroupedByHash = new List<IEnumerable<M365File>>();
            foreach (IEnumerable<M365File> fileGroup in fileGroups)
            {
                Dictionary<string, List<M365File>> newFileGroups = DuplicateFileFinder.GroupFilesByHash(fileGroup, groupingKeySelector);
                foreach (IEnumerable<M365File> newFileGroup in newFileGroups.Values)
                {
                    if (newFileGroup.Count() > 1)
                    {
                        filesGroupedByHash.Add(newFileGroup);
                    }
                }
            }
            fileGroups = filesGroupedByHash;
        }

        return fileGroups;
    }

    private static string GetM365FileQuickXorHash(M365File file)
    {
        return file.QuickXorHash ?? DuplicateFileFinder.s_emptyHash;
    }

    private static string GetM365FileSha1Hash(M365File file)
    {
        return file.Sha1Hash ?? DuplicateFileFinder.s_emptyHash;
    }

    private static string GetM365FileCrc32Hash(M365File file)
    {
        return file.Crc32Hash ?? DuplicateFileFinder.s_emptyHash;
    }

    private static Dictionary<string, List<M365File>> GroupFilesByHash(IEnumerable<M365File> files, Func<M365File, string> hashSelector)
    {
        Dictionary<string, List<M365File>> filesGroupedByHash = DuplicateFileFinder.GroupFiles<string>(files, hashSelector);

        // Hash may not be populated, files with no hash will be treated as potential duplicate of all other files.
        DuplicateFileFinder.MoveFilesInEmptyHashGroupToOtherGroups(filesGroupedByHash);

        return filesGroupedByHash;
    }

    private static void MoveFilesInEmptyHashGroupToOtherGroups(Dictionary<string, List<M365File>> fileGroups)
    {
        if (fileGroups.TryGetValue(DuplicateFileFinder.s_emptyHash, out List<M365File>? filesWithEmptyHash) && fileGroups.Count > 1)
        {
            fileGroups.Remove(DuplicateFileFinder.s_emptyHash);

            foreach (KeyValuePair<string, List<M365File>> fileGroup in fileGroups)
            {
                foreach (M365File file in filesWithEmptyHash)
                {
                    fileGroup.Value.Add(file);
                }
            }
        }
    }
}