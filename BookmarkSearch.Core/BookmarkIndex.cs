using System;
using System.Collections.Generic;

namespace BookmarkSearch.Core;

public sealed record BookmarkEntry(string Name, string Url, IReadOnlyList<string> FolderPath)
{
    public string FolderDisplay => string.Join('/', FolderPath);

    public string DisplayUrl => UrlDisplay.Trim(Url);
}

public sealed class BookmarkFolder
{
    private readonly List<BookmarkFolder> _folders = [];
    private readonly List<BookmarkEntry> _bookmarks = [];

    public BookmarkFolder(string name, IReadOnlyList<string> path)
    {
        Name = name;
        Path = path;
    }

    public string Name { get; }

    public IReadOnlyList<string> Path { get; }

    public string PathDisplay => string.Join('/', Path);

    public IReadOnlyList<BookmarkFolder> Folders => _folders;

    public IReadOnlyList<BookmarkEntry> Bookmarks => _bookmarks;

    internal BookmarkFolder GetOrAddFolder(string name)
    {
        // Folders with the same name (e.g. from two profiles) are merged.
        foreach (var folder in _folders)
        {
            if (folder.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return folder;
            }
        }

        var created = new BookmarkFolder(name, [.. Path, name]);
        _folders.Add(created);
        return created;
    }

    internal void AddBookmark(BookmarkEntry entry) => _bookmarks.Add(entry);
}

/// <summary>
/// In-memory folder tree plus a flat list of every bookmark, in source order.
/// One index serves both tree and flat navigation.
/// </summary>
public sealed class BookmarkIndex
{
    private readonly List<BookmarkEntry> _all = [];

    public BookmarkFolder Root { get; } = new(string.Empty, []);

    public IReadOnlyList<BookmarkEntry> AllBookmarks => _all;

    public BookmarkFolder AddFolder(IReadOnlyList<string> path)
    {
        var folder = Root;
        foreach (var name in path)
        {
            folder = folder.GetOrAddFolder(name);
        }

        return folder;
    }

    public void AddBookmark(string name, string url, IReadOnlyList<string> folderPath)
    {
        var folder = AddFolder(folderPath);
        var entry = new BookmarkEntry(name, url, folder.Path);
        folder.AddBookmark(entry);
        _all.Add(entry);
    }
}

public static class UrlDisplay
{
    /// <summary>
    /// Shortens a URL for display only: drops http(s):// and a leading "www." on the host.
    /// Path, query and trailing slash are kept.
    /// </summary>
    public static string Trim(string url)
    {
        var text = url;
        if (text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            text = text["https://".Length..];
        }
        else if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            text = text["http://".Length..];
        }
        else
        {
            return url;
        }

        if (text.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            text = text["www.".Length..];
        }

        return text;
    }
}
