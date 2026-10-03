namespace HPRebar.Core.Shared;

/// <summary>What Revit accepts as a view name.</summary>
public static class RevitViewNames
{
    /// <summary>Characters Revit refuses in a view name.</summary>
    public const string ForbiddenCharacters = "\\:{}[]|;<>?`~";

    /// <summary>The first character of <paramref name="name"/> that Revit would refuse, when there is one.</summary>
    public static bool TryFindForbiddenCharacter(string name, out char character)
    {
        foreach (var candidate in name)
        {
            if (ForbiddenCharacters.IndexOf(candidate) < 0) continue;

            character = candidate;
            return true;
        }

        character = default;
        return false;
    }
}
