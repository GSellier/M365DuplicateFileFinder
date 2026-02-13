using M365DuplicateFileFinder;
using M365DuplicateFileFinder.Models;
using M365DuplicateFileFinder.Readers;

IEnumerable<IEnumerable<M365File>> duplicateFileGroups;

using (var graphReader = new GraphFileReader())
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