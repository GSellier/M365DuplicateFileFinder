using M365DuplicateFileFinder;
using M365DuplicateFileFinder.Models;
using M365DuplicateFileFinder.Readers;

// Ids of folders to analyze (not needed if everything should be analyzed).
string[] testFolderIds =
    [
        "01234567890ABCDEF!s549db67ecbf44cb18e0ff854efe005c5"
    ];

IEnumerable<IEnumerable<M365File>> duplicateFileGroups;

using
(
    var graphReader = new GraphFileReader
    {
        // Uncomment the next line to analyze specific folders only. If nothing is specified, everything under the root folder will be analyzed.
        //IdsOfFoldersToQuery = testFolderIds
    }
)
{
    var duplicateFileFinder = new DuplicateFileFinder
    {
        FileReader = graphReader
    };

    duplicateFileGroups = await duplicateFileFinder.GetDuplicateFilesAsync();
}

Console.WriteLine(duplicateFileGroups.Count() + " potential duplicate groups found.");
int groupNumber = 0;
foreach (IEnumerable<M365File> duplicateFileGroup in duplicateFileGroups)
{
    Console.WriteLine("Group {0}:", ++groupNumber);
    foreach (M365File file in duplicateFileGroup)
    {
        Console.WriteLine(
            "{0} {1} {2} {3} {4} {5}B {6} {7} {8}",
            file.Name,
            file.WebUrl,
            file.ParentPath,
            file.CreatedDateTime,
            file.LastModifiedDateTime,
            file.Size,
            file.QuickXorHash,
            file.Sha1Hash,
            file.Crc32Hash);
    }
    Console.WriteLine();
}