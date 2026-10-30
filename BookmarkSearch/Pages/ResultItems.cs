using System;
using BookmarkSearch.Core;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BookmarkSearch;

/// <summary>
/// Maps search results to list items.
/// </summary>
internal static class ResultItems
{
    public static readonly IconInfo FolderIcon = new("");
    public static readonly IconInfo BookmarkIcon = new("");

    // No leading "/": the "/" alias opens the page with an empty search box.
    public static string FolderQuery(SearchResult folder) => $"{string.Join('/', folder.FolderPath)}/";

    public static bool CanOpen(SearchResult result) =>
        result.Kind == ResultKind.Folder ||
        !(result.Url ?? string.Empty).StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);

    public static ListItem Bookmark(SearchResult result)
    {
        var url = result.Url ?? string.Empty;
        return new ListItem(new OpenUrlCommand(url) { Name = "Open", Result = CommandResult.Dismiss() })
        {
            Title = result.Title.Length > 0 ? result.Title : result.Subtitle,
            Subtitle = result.Subtitle,
            Icon = BookmarkIcon,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(url) { Name = "Copy URL" })],
        };
    }

    public static ListItem Folder(SearchResult result, BookmarkSearchPage page)
    {
        var query = FolderQuery(result);
        return new ListItem(new BrowseFolderCommand(page, query))
        {
            Title = result.Title,
            Subtitle = "/" + result.Subtitle,
            Icon = FolderIcon,

            // Tab accepts the suggestion and completes the folder path.
            TextToSuggest = query,
        };
    }
}
