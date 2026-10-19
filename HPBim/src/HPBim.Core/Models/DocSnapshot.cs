namespace HPBim.Core.Models;

using System;
using HPBim.Core.Execution;

public sealed record DocSnapshot
{
    public string Title { get; init; } = string.Empty;
    public string? PathName { get; init; }
    public Guid? CentralGuid { get; init; }
    public Guid? ProjectGuid { get; init; }
    public Guid VersionGuid { get; init; } = Guid.Empty;
    public int NumberOfSaves { get; init; }

    public bool HasStaticIdentity =>
        !string.IsNullOrEmpty(Title) ||
        !string.IsNullOrEmpty(PathName) ||
        (CentralGuid.HasValue && CentralGuid.Value != Guid.Empty) ||
        (ProjectGuid.HasValue && ProjectGuid.Value != Guid.Empty);

    public bool MatchesStaticIdentity(string actualTitle, string? actualPath, string? actualCentralGuid = null, string? actualProjectGuid = null)
    {
        if (!string.IsNullOrEmpty(Title))
        {
            if (string.IsNullOrEmpty(actualTitle) ||
                !string.Equals(Title, actualTitle, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(PathName))
        {
            if (string.IsNullOrEmpty(actualPath) ||
                !string.Equals(PathName, actualPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (CentralGuid.HasValue && CentralGuid.Value != Guid.Empty)
        {
            if (string.IsNullOrEmpty(actualCentralGuid) ||
                !Guid.TryParse(actualCentralGuid, out var centGuid) ||
                centGuid == Guid.Empty ||
                CentralGuid.Value != centGuid)
            {
                return false;
            }
        }

        if (ProjectGuid.HasValue && ProjectGuid.Value != Guid.Empty)
        {
            if (string.IsNullOrEmpty(actualProjectGuid) ||
                !Guid.TryParse(actualProjectGuid, out var projGuid) ||
                projGuid == Guid.Empty ||
                ProjectGuid.Value != projGuid)
            {
                return false;
            }
        }

        return true;
    }

    public static DocSnapshot FromDocumentIdentitySnapshot(DocumentIdentitySnapshot? snapshot)
    {
        if (snapshot == null)
        {
            return new DocSnapshot();
        }

        return new DocSnapshot
        {
            Title = snapshot.Title,
            PathName = snapshot.PathName,
            CentralGuid = snapshot.CentralGuid,
            VersionGuid = snapshot.VersionGuid,
            NumberOfSaves = snapshot.NumberOfSaves
        };
    }

    public DocumentIdentitySnapshot ToDocumentIdentitySnapshot()
    {
        return new DocumentIdentitySnapshot
        {
            Title = Title,
            PathName = PathName,
            CentralGuid = CentralGuid,
            VersionGuid = VersionGuid,
            NumberOfSaves = NumberOfSaves
        };
    }
}
