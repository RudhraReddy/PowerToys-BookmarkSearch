// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BookmarkSearch;

public partial class BookmarkSearchCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settingsManager = new();
    private readonly BookmarkStore _store = new();

    public BookmarkSearchCommandsProvider()
    {
        DisplayName = "Bookmark Search";
        Icon = ResultItems.AppIcon;

        _store.SetFiles(_settingsManager.BookmarkFiles);
        _settingsManager.Changed += (_, _) => _store.SetFiles(_settingsManager.BookmarkFiles);

        var page = new BookmarkSearchPage(_settingsManager, _store);
        _commands = [
            new CommandItem(page)
            {
                Title = DisplayName,
                Subtitle = "Search Chrome, Edge and Brave bookmarks",
                MoreCommands = [new CommandContextItem(_settingsManager.Settings.SettingsPage)],
            },
        ];

        Settings = _settingsManager.Settings;
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }
}
