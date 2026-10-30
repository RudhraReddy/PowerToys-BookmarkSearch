// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using BookmarkSearch.Core;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BookmarkSearch;

internal sealed partial class BookmarkSearchPage : DynamicListPage
{
    // Stable id so a Command Palette alias ("/") can point at this page.
    public const string CommandId = "com.rudhrareddy.bookmarksearch";

    private const int MaxResults = 300;

    private readonly SettingsManager _settings;
    private readonly BookmarkStore _store;
    private IListItem[] _items = [];

    public BookmarkSearchPage(SettingsManager settings, BookmarkStore store)
    {
        _settings = settings;
        _store = store;

        Id = CommandId;
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Bookmark Search";
        Name = "Open";
        PlaceholderText = "Search bookmarks, or folder/name";
        EmptyContent = new CommandItem(new NoOpCommand()) { Title = "No matching bookmarks", Icon = ResultItems.BookmarkIcon };

        _store.Changed += (_, _) => Refresh();
        _settings.Changed += (_, _) => Refresh();
        Refresh();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => Refresh();

    public override IListItem[] GetItems() => _items;

    /// <summary>
    /// Replaces the search box text, e.g. when entering a folder.
    /// </summary>
    public void SetQuery(string query)
    {
        SearchText = query;
        OnPropertyChanged(nameof(SearchText));
    }

    private void Refresh()
    {
        _items = BookmarkSearcher.Search(_store.Index, SearchText, _settings.NavigationMode)
            .Where(ResultItems.CanOpen)
            .Take(MaxResults)
            .Select(r => r.Kind == ResultKind.Folder ? ResultItems.Folder(r, this) : (IListItem)ResultItems.Bookmark(r))
            .ToArray();

        RaiseItemsChanged(_items.Length);
    }
}
