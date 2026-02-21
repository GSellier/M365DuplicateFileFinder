using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using M365DuplicateFileFinder.Models;

namespace M365DuplicateFileFinder.Readers;

/// <summary>
/// Provides methods to read files from a library using the Graph API.
/// </summary>
public class GraphFileReader : IFileReader, IDisposable
{
    private const string ClientId = "4b9776fd-6e9e-4ab6-b8e4-471749de2151";
    private const string TenantType_Consumer = "consumers";
    private const string TenantType_Common = "common";
    private static readonly Uri s_redirectUri = new("http://localhost");
    private static readonly string[] s_scopes = ["Files.Read"];

    private static readonly string[] s_driveIdSelectParameters = ["id"];
    private const string DriveRootItemId = "root";
    private static readonly string[] s_driveItemsSelectParameters =
        [
            "id",
            "file",
            "folder",
            "size",
            "name",
            "webUrl",
            "parentReference",
            "createdDateTime",
            "lastModifiedDateTime"
        ];

    private readonly GraphServiceClient _graphClient;
    private GraphServiceClient GraphClient
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, _graphClient);
            return _graphClient;
        }
    }

    private bool _disposed = false;

    private readonly List<M365File> _files;
    private readonly Stack<string> _nonEmptyFolderIds;

    /// <summary>
    /// Gets or initializes the list of ids of the folders to read (the ids of the driveItems). The default value will be the id of the root folder.
    /// </summary>
    public string[] IdsOfFoldersToQuery { get; init; }

    /// <summary>
    /// Initializes a new instance of the GraphFileReader class.
    /// Initializes the connection to the Graph API using the interactive provider for a consumer account.
    /// </summary>
    public GraphFileReader()
    {
        _graphClient = GraphFileReader.CreateGraphClientWithInteractiveProvider();
        _files = [];
        _nonEmptyFolderIds = new Stack<string>();
        IdsOfFoldersToQuery = [GraphFileReader.DriveRootItemId];
    }

    /// <summary>
    /// Releases disposable resources used by the GraphFileReader object.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases disposable resources used by the GraphFileReader object.
    /// </summary>
    /// <param name="disposing">
    /// true to release resources; false will not release anything (the class does not use unmanaged resources directly).
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
            
        if (disposing)
            _graphClient?.Dispose();
        
        _disposed = true;
    }

    private static GraphServiceClient CreateGraphClientWithInteractiveProvider()
    {
        var options = new InteractiveBrowserCredentialOptions
        {
            TenantId = GraphFileReader.TenantType_Common,
            ClientId = GraphFileReader.ClientId,
            RedirectUri = GraphFileReader.s_redirectUri
        };

        var interactiveCredential = new InteractiveBrowserCredential(options);

        return new GraphServiceClient(interactiveCredential, GraphFileReader.s_scopes);
    }

    /// <summary>
    /// Gets the files.
    /// All files will be retreived, including those in subfolders.
    /// The analysis will start at the root folder, or from those specified in IdsOfFoldersToQuery during object intialization. Then all their
    /// subfolders will be read as well.
    /// </summary>
    /// <returns>
    /// A Task object representing the asynchronous operation. The result of the Task is the list of files.
    /// </returns>
    public async Task<IEnumerable<M365File>> GetFilesAsync()
    {
        string driveId = await GetDriveIdAsync();
        return await GetFilesAsync(driveId, IdsOfFoldersToQuery);
    }

    private async Task<string> GetDriveIdAsync()
    {
        Drive? drive = await GraphClient.Me.Drive
            .GetAsync(
                static (requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = GraphFileReader.s_driveIdSelectParameters;
                }
            );
        return drive?.Id ?? throw new InvalidOperationException();
    }

    /// <summary>
    /// Get all files under the specified parent folders.
    /// Each subfolder is queried individually. Getting all the files at once would be much faster, but it is not documented and has a strange
    /// behavior.
    /// </summary>
    private async Task<IEnumerable<M365File>> GetFilesAsync(string driveId, string[] parentDriveItemIds)
    {
        _files.Clear();

        foreach (string folderId in parentDriveItemIds)
            _nonEmptyFolderIds.Push(folderId);

        while (_nonEmptyFolderIds.Count > 0)
        {
            string driveItemId = _nonEmptyFolderIds.Pop();
            await FetchFilesAndNonEmptyFoldersAsync(driveId, driveItemId);
        }

        return _files;
    }

    private async Task FetchFilesAndNonEmptyFoldersAsync(string driveId, string driveItemId)
    {
        DriveItemCollectionResponse? driveItemCollectionResponse = await GraphClient.Drives[driveId].Items[driveItemId].Children
            .GetAsync(
                static (requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = GraphFileReader.s_driveItemsSelectParameters;
                }
            ) ?? throw new InvalidOperationException();

        PageIterator<DriveItem, DriveItemCollectionResponse> pageIterator = PageIterator<DriveItem, DriveItemCollectionResponse>
            .CreatePageIterator(
                GraphClient,
                driveItemCollectionResponse,
                (driveItem) =>
                {
                    if (driveItem.File != null)
                    {
                        M365File file = GraphFileReader.CreateM365File(driveItem);
                        _files.Add(file);
                    }
                    else if (driveItem.Folder?.ChildCount > 0)
                    {
                        if (driveItem.Id == null)
                        {
                            throw new InvalidOperationException();
                        }
                        else
                        {
                            _nonEmptyFolderIds.Push(driveItem.Id);
                        }
                    }
                    return true;
                }
            );
        await pageIterator.IterateAsync();
    }

    private static M365File CreateM365File(DriveItem driveItem)
    {
        Hashes hashes = driveItem.File?.Hashes ?? throw new InvalidOperationException();
        return new M365File
        {
            Id = driveItem.Id,
            Name = driveItem.Name,
            WebUrl = driveItem.WebUrl,
            ParentPath = driveItem.ParentReference?.Path,
            CreatedDateTime = driveItem.CreatedDateTime,
            LastModifiedDateTime = driveItem.LastModifiedDateTime,
            Size = driveItem.Size ?? throw new InvalidOperationException(),
            QuickXorHash = hashes.QuickXorHash,
            Sha1Hash = hashes.Sha1Hash,
            Crc32Hash = hashes.Crc32Hash
        };
    }
}