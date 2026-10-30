using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BookmarkSearch.Core;

/// <summary>
/// Reads a Chromium "Bookmarks" JSON file (Chrome, Edge, Brave) into a <see cref="BookmarkIndex"/>.
/// </summary>
public static class ChromeBookmarkReader
{
    // Root names are not part of folder paths. Unknown roots (e.g. Edge's "workspaces_v2") are skipped.
    private static readonly string[] RootNames = ["bookmark_bar", "other", "synced"];

    public static void ReadFile(string path, BookmarkIndex index)
    {
        // The browser keeps the file open while running, so read a snapshot with shared access.
        byte[] data;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            data = buffer.ToArray();
        }

        Parse(data, index);
    }

    public static void Parse(ReadOnlyMemory<byte> json, BookmarkIndex index)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("roots", out var roots) || roots.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var path = new List<string>();
        foreach (var rootName in RootNames)
        {
            if (roots.TryGetProperty(rootName, out var root))
            {
                AddChildren(root, path, index);
            }
        }
    }

    private static void AddChildren(JsonElement folder, List<string> path, BookmarkIndex index)
    {
        if (!folder.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var child in children.EnumerateArray())
        {
            var name = GetString(child, "name");
            if (GetString(child, "type") == "url")
            {
                var url = GetString(child, "url");
                if (url.Length > 0)
                {
                    index.AddBookmark(name, url, path.ToArray());
                }
            }
            else if (child.TryGetProperty("children", out _))
            {
                path.Add(name);
                index.AddFolder(path.ToArray());
                AddChildren(child, path, index);
                path.RemoveAt(path.Count - 1);
            }
        }
    }

    private static string GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}

public static class BookmarkSources
{
    private static readonly string[] UserDataDirectories =
    [
        @"Google\Chrome\User Data",
        @"Microsoft\Edge\User Data",
        @"BraveSoftware\Brave-Browser\User Data",
    ];

    /// <summary>
    /// Finds every profile's Bookmarks file for the known Chromium browsers.
    /// </summary>
    public static IReadOnlyList<string> Discover(string localAppData)
    {
        var files = new List<string>();
        foreach (var relative in UserDataDirectories)
        {
            var userData = Path.Combine(localAppData, relative);
            if (!Directory.Exists(userData))
            {
                continue;
            }

            var profiles = Directory.GetDirectories(userData);
            Array.Sort(profiles, CompareProfiles);
            foreach (var profile in profiles)
            {
                var file = Path.Combine(profile, "Bookmarks");
                if (File.Exists(file))
                {
                    files.Add(file);
                }
            }
        }

        return files;
    }

    // "Default" first, then "Profile 1", "Profile 2", ...
    private static int CompareProfiles(string a, string b)
    {
        var aDefault = Path.GetFileName(a).Equals("Default", StringComparison.OrdinalIgnoreCase);
        var bDefault = Path.GetFileName(b).Equals("Default", StringComparison.OrdinalIgnoreCase);
        if (aDefault != bDefault)
        {
            return aDefault ? -1 : 1;
        }

        return StringComparer.OrdinalIgnoreCase.Compare(a, b);
    }
}
