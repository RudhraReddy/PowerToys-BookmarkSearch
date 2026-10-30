// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BookmarkSearch.Core;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BookmarkSearch;

internal sealed partial class SettingsManager : JsonSettingsManager
{
    private static readonly string _namespace = "BookmarkSearch";

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    private readonly ChoiceSetSetting _navigationMode = new(
        Namespaced(nameof(NavigationMode)),
        "Navigation mode",
        "Tree walks folders with / (e.g. /anime/an1). Flat searches every bookmark by name and ignores folders.",
        [
            new ChoiceSetSetting.Choice("Tree", "tree"),
            new ChoiceSetSetting.Choice("Flat", "flat"),
        ]);

    private readonly TextSetting _bookmarkFiles = new(
        Namespaced(nameof(BookmarkFiles)),
        "Bookmark files",
        "One Bookmarks file path per line. Leave empty to find Chrome, Edge and Brave profiles automatically.",
        string.Empty)
    {
        Multiline = true,
        Placeholder = @"%LOCALAPPDATA%\Google\Chrome\User Data\Default\Bookmarks",
    };

    public event EventHandler? Changed;

    public NavigationMode NavigationMode =>
        string.Equals(_navigationMode.Value, "flat", StringComparison.OrdinalIgnoreCase) ? NavigationMode.Flat : NavigationMode.Tree;

    public IReadOnlyList<string> BookmarkFiles
    {
        get
        {
            var configured = (_bookmarkFiles.Value ?? string.Empty)
                .Split('\n')
                .Select(line => Environment.ExpandEnvironmentVariables(line.Trim().Trim('"')))
                .Where(line => line.Length > 0)
                .ToArray();

            return configured.Length > 0
                ? configured
                : BookmarkSources.Discover(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }
    }

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("BookmarkSearch");
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "settings.json");
    }

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        Settings.Add(_navigationMode);
        Settings.Add(_bookmarkFiles);

        // Load settings from file upon initialization
        LoadSettings();

        Settings.SettingsChanged += (_, _) =>
        {
            SaveSettings();
            Changed?.Invoke(this, EventArgs.Empty);
        };
    }
}
