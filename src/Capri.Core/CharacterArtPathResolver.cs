namespace Capri.Core;

/// <summary>
/// Finds the complete state frames the desktop should render. Custom local
/// animation takes priority; the separately licensed packaged art is the
/// immediate fallback. No filesystem mutation or network call is performed.
/// </summary>
public static class CharacterArtPathResolver
{
    public static string FindStateFolder(string customRoot, string defaultRoot, string state)
    {
        if (!CharacterPackImporter.States.Contains(state, StringComparer.Ordinal))
            throw new ArgumentException("Unknown character state.", nameof(state));
        var userFolder = Path.Combine(customRoot, state);
        var defaultFolder = Path.Combine(defaultRoot, state);
        if (HasFrames(userFolder)) return userFolder;
        if (HasFrames(defaultFolder)) return defaultFolder;
        return userFolder;
    }

    private static bool HasFrames(string folder)
    {
        try
        {
            return Directory.Exists(folder) &&
                Directory.EnumerateFiles(folder, "*.png").Any();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
