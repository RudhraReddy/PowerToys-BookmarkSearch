using System;
using System.Collections.Generic;
using System.Linq;

namespace BookmarkSearch.Core;

public enum NavigationMode
{
    Tree,
    Flat,
}

public enum ResultKind
{
    Folder,
    Bookmark,
}

public sealed record SearchResult(ResultKind Kind, string Title, string Subtitle, string? Url, IReadOnlyList<string> FolderPath)
{
    public static SearchResult From(BookmarkEntry entry) =>
        new(ResultKind.Bookmark, entry.Name, entry.DisplayUrl, entry.Url, entry.FolderPath);

    public static SearchResult From(BookmarkFolder folder) =>
        new(ResultKind.Folder, folder.Name, folder.PathDisplay, null, folder.Path);
}

/// <summary>
/// "/anime/an1" -> folders ["anime"], term "an1". "/anime/" -> folders ["anime"], term "".
/// The leading "/" is optional.
/// </summary>
public sealed record ParsedQuery(IReadOnlyList<string> Folders, string Term)
{
    public static ParsedQuery Parse(string input)
    {
        var text = input.Trim();
        if (text.StartsWith('/'))
        {
            text = text[1..];
        }

        var segments = text.Split('/');
        var folders = segments[..^1]
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        return new ParsedQuery(folders, segments[^1].Trim());
    }
}

public static class Matching
{
    /// <summary>
    /// exact (3) > prefix (2) > substring (1) > no match (0). Case-insensitive.
    /// An empty term matches everything equally.
    /// </summary>
    public static int Score(string name, string term)
    {
        if (term.Length == 0)
        {
            return 1;
        }

        if (name.Equals(term, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (name.StartsWith(term, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return name.Contains(term, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }
}

public static class BookmarkSearcher
{
    public static IReadOnlyList<SearchResult> Search(BookmarkIndex index, string input, NavigationMode mode)
    {
        var query = ParsedQuery.Parse(input);
        if (mode == NavigationMode.Flat)
        {
            // Flat mode ignores typed folder segments and never shows folders.
            return Rank(index.AllBookmarks, b => b.Name, query.Term).Select(SearchResult.From).ToList();
        }

        List<BookmarkFolder> current = [index.Root];
        foreach (var segment in query.Folders)
        {
            current = BestMatches(current.SelectMany(f => f.Folders), segment);
            if (current.Count == 0)
            {
                return [];
            }
        }

        // Folders first, then bookmarks.
        var folders = Rank(current.SelectMany(f => f.Folders), f => f.Name, query.Term).Select(SearchResult.From);
        var bookmarks = Rank(current.SelectMany(f => f.Bookmarks), b => b.Name, query.Term).Select(SearchResult.From);
        return [.. folders, .. bookmarks];
    }

    // Stable: ties keep source order, so the list does not reshuffle while typing.
    private static IEnumerable<T> Rank<T>(IEnumerable<T> items, Func<T, string> name, string term) =>
        items
            .Select(item => (Item: item, Score: Matching.Score(name(item), term)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Item);

    // A folder segment walks into the best-matching folders at that level,
    // so "/ani/" works when "anime" is the best match.
    private static List<BookmarkFolder> BestMatches(IEnumerable<BookmarkFolder> folders, string segment)
    {
        var scored = folders
            .Select(f => (Folder: f, Score: Matching.Score(f.Name, segment)))
            .Where(x => x.Score > 0)
            .ToList();
        if (scored.Count == 0)
        {
            return [];
        }

        var best = scored.Max(x => x.Score);
        return scored.Where(x => x.Score == best).Select(x => x.Folder).ToList();
    }
}
