namespace ToJePrivela.Api.Tests.Common;

/// <summary>The repository's files that carry the game's version, read from the checkout the tests run in.</summary>
public static class GameVersion
{
    /// <summary>The version in <c>VERSION</c>, the one source every build takes it from.</summary>
    public static string Read() => File.ReadAllText(PathOf("VERSION")).Trim();

    /// <summary>A file by its path from the repository root (the folder holding <c>ToJePrivela.slnx</c>).</summary>
    public static string PathOf(string relativePath)
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "ToJePrivela.slnx")))
            {
                return Path.Combine(folder.FullName, relativePath);
            }
        }

        throw new InvalidOperationException("The tests do not run inside the repository.");
    }
}
