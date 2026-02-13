namespace M365DuplicateFileFinder.Models;

/// <summary>
/// Represents a file stored in Microsoft 365.
/// </summary>
public class M365File
{
    public required string? Id { get; init; }
    public required string? Name { get; init; }
    public required string? WebUrl { get; init; }
    public required string? ParentPath { get; init; }
    public required DateTimeOffset? CreatedDateTime { get; init; }
    public required DateTimeOffset? LastModifiedDateTime { get; init; }
    public required long Size { get; init; }
    public required string? QuickXorHash { get; init; }
    public required string? Sha1Hash { get; init; }
    public required string? Crc32Hash { get; init; }
}