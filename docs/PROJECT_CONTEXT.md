# Bookmark Navigator: Project Context and Decisions

## 1. Goal

A PowerToys **Command Palette** extension that searches Google Chrome bookmarks and opens them from the palette, with keyboard-driven folder navigation and a settings page.

## 2. Platform and Stack

| Item | Decision |
|---|---|
| Host | PowerToys Command Palette (CmdPal). Chosen over PowerToys Run. |
| Language | C# |
| Runtime | .NET (use the version the extension template targets) |
| Packaging | MSIX (Command Palette extensions are packaged this way) |
| IDE | Visual Studio with .NET desktop and Windows application development workloads |
| Project creation | Command Palette: `Win+Alt+Space`, type "Create extension", open the generated `.sln` |
| Dev loop | Build and deploy from Visual Studio, run `Reload` in Command Palette, attach debugger to the extension process |
| Libraries | `System.Text.Json`, `System.IO`, `System.Uri` (all in-box). Tests use xUnit. |
| Search | In-memory list. No database or search library. |

**Known risk:** Command Palette is in preview and its API may change before v1.0.0. Check generated template names (`ListPage`, search text, settings APIs) against the actual template before copying code.

## 3. Bookmark Source

- Chrome stores bookmarks per profile in a JSON file named `Bookmarks`.
- Windows path: `%LOCALAPPDATA%\Google\Chrome\User Data\<Profile>\Bookmarks`
  - `<Profile>` is usually `Default`, or `Profile 1`, `Profile 2`, etc.
- Bookmark roots to read: `bookmark_bar`, `other`, `synced`. Root names are **not** included in folder paths.
- Non-`url` nodes (folders) recurse via `children`. Only `type: "url"` nodes become bookmarks.
- Copy the file to a temp location before reading. Chrome holds the original while running.
- Settings will hold a **list of bookmark file paths** (supports multiple profiles).
- Watch the file (`FileSystemWatcher`) or re-index on a timer.

### 3.1 Verified on the dev machine (2026-10-30)

- Chrome: `%LOCALAPPDATA%\Google\Chrome\User Data\Default\Bookmarks` (found on a later check; the first check missed it).
- **Format is JSON**, not SQLite. Confirmed on Chrome, Edge and Brave:
  - Edge: `%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Bookmarks`. Roots: `bookmark_bar`, `other`, `synced`, plus an extra Edge-only `workspaces_v2`.
  - Brave: `%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\Default\Bookmarks`. Roots: `bookmark_bar`, `other`, `synced`.
- The parser should read only the three known roots and ignore unknown ones like `workspaces_v2`.
- If the bookmark files setting is empty, the extension auto-detects every profile of Chrome, Edge and Brave (`BookmarkSources.Discover`).
- The file is read with shared access into memory instead of copying it to a temp file. Same effect, no temp files.
- Smoke test on the dev machine: 3 files, 140 bookmarks, 11 top-level folders.

## 4. User-Facing Behavior

### 4.1 Trigger and Query Syntax
- Typing `/` in Command Palette directly activates Bookmark Search, the same way built-in prefixes work (`??` web search, `>` run, `=` calculator, `$` settings).
- CmdPal hard-codes default aliases only for built-in commands (`AliasManager._defaultAliases`). Extensions cannot register one, so the user sets it once: CmdPal settings > Extensions > Bookmark Search > Alias `/`, Direct on.
- The alias points at the page's stable command id `com.rudhrareddy.bookmarksearch`.
- Activation clears the box, so queries inside the page have no leading `/`. A leading `/` is still accepted.
- Empty input shows top-level folders (tree mode) or all bookmarks (flat mode).
- Trailing `/` browses folder contents (tree mode): `anime/` shows contents of `anime`.
- Last segment is a search term matched against names.
- Example tree query: `/` then `anime/an1`.
- Example flat query: `/` then `an1` finds every bookmark named like "an1", in any folder.

### 4.2 Navigation Mode (global setting)
- **Tree:** each `/` segment walks one folder level. Matching is per level.
- **Flat:** folders are ignored for matching. All bookmarks are searched. Folders are never shown as results. Typed folder segments are ignored (`/anime/an1` behaves like `/an1`).
- Mode is a **single global setting**, not per source.
- One shared index serves both modes. Switching mode does not re-read the Chrome file.

### 4.3 Result List
- Live update on every keystroke.
- Row layout: **left = name** (bookmark title or folder name), **right = display URL** (bookmarks) or path (folders).
- Up/Down move selection. Enter opens the highlighted item.
- Duplicate names are allowed. The right column is what distinguishes them.

### 4.4 Display URL Rule
- Strip `https://` or `http://`.
- Strip a leading `www.` only at the start of the host (`blog.www.example.com` stays intact).
- Keep path and query. Leave trailing slash as-is.
- **Display only.** Stored and opened URL is always the full original.
- Full URL should be available on hover or in a details view.

### 4.5 Tab Completion (tree mode only)
- CmdPal does not pass the Tab key to extensions. Instead, each folder item sets `ListItem.TextToSuggest` to `<path>/`, and the host's Tab accepts the suggestion for the selected item.
- Enter on a folder also walks into it (replaces the search text with `<path>/`).
- No common-prefix completion or Tab cycling; Up/Down picks the folder first.
- Flat mode has no folder items, so nothing to complete.
- **Not verified live yet:** that Tab accepts `TextToSuggest` and that setting `SearchText` from the extension updates the search box.

### 4.6 Ranking
- Score: exact (3) > prefix (2) > substring (1). No match is excluded.
- Matching is case-insensitive.
- Ties keep the source order (stable sort). The list should not reshuffle while typing.
- Tree mode: **folders listed before bookmarks** so Tab completion stays predictable.

### 4.7 Opening Items
- Bookmarks open in the default browser via `Process.Start` with `UseShellExecute = true`.

## 5. Code Layout

```
BookmarkSearch.sln
BookmarkSearch/                     Command Palette extension (from the CmdPal template, PowerToys v0.102.3001)
  BookmarkSearch.cs                 IExtension / COM entry (template)
  BookmarkSearchCommandsProvider.cs top-level command, settings
  Program.cs                        COM server host (template, do not change)
  Helpers/SettingsManager.cs        navigation mode, bookmark files
  Helpers/BookmarkStore.cs          loads files into the index, FileSystemWatcher re-index
  Pages/BookmarkSearchPage.cs       DynamicListPage: search on every keystroke; stable Id for the "/" alias
  Pages/ResultItems.cs              SearchResult -> ListItem
  Commands/BrowseFolderCommand.cs   Enter on a folder walks into it
BookmarkSearch.Core/                UI-independent logic (net10.0, AOT-safe)
  BookmarkIndex.cs                  BookmarkEntry, BookmarkFolder, BookmarkIndex, UrlDisplay
  ChromeBookmarkReader.cs           reads the Bookmarks JSON; BookmarkSources.Discover
  BookmarkSearcher.cs               ParsedQuery, Matching, Search(index, input, mode)
BookmarkSearch.Tests/               xUnit tests for Core
scripts/deploy.ps1                  build + register the package without Visual Studio
```

Key types:
- `BookmarkEntry(Name, Url, FolderPath)`, with `FolderDisplay` and `DisplayUrl`.
- `SearchResult(Kind, Title, Subtitle, Url, FolderPath)`. `Kind` is `Folder` or `Bookmark`.
- `NavigationMode`: `Tree` | `Flat`.
- `ParsedQuery(Folders, Term)`. A trailing `/` simply gives an empty term.

Toolkit notes (package `Microsoft.CommandPalette.Extensions` 0.11.260520004):
- `StringListSetting` / `FilePathListSetting` / `KeyValueListSetting` exist in newer PowerToys source but **not** in this package, so the bookmark files setting is a multi-line `TextSetting`.
- There is no API for an extension to set a default alias (see 4.1).

## 6. Work Completed

1. Decided on Command Palette over PowerToys Run.
2. Designed query grammar, tree/flat modes, result layout, Tab behavior, display-URL rule.
3. Wrote `ChromeBookmarkReader` and `BookmarkEntry` (parser, root-name stripping, temp-copy read).
4. Wrote `QueryParser`, `Matching`, and `BookmarkSearch` (search and Tab completion).
5. Wrote xUnit tests for core search behavior.
6. Verified the bookmark file format locally (JSON) on Chrome, Edge and Brave. See 3.1.
7. Generated the extension project from the CmdPal template; it builds with the .NET 10 SDK and Windows SDK 26100.
8. Rewrote Core (index, reader, search) and 15 xUnit tests; all pass.
9. Wrote the list page, settings and FileSystemWatcher re-indexing. Deployed and working in Command Palette.
10. Dropped the shortcut-prefix feature: `@` and `??` in the original notes were examples of CmdPal's own prefixes, not features to build.

## 7. Next Steps

1. Set the `/` alias (Direct) in CmdPal settings and confirm `/` opens Bookmark Search.
2. Verify live: Tab on folders (`TextToSuggest`), Enter on folders updates the search box, settings save and re-index.
3. Packaging and submission, if pursued (Store certification typically up to three business days).

## 8. Open Questions

- Should flat mode narrow by typed folder segments, or always ignore them? (Currently: ignore.)
- Should non-web URLs (`javascript:`, `chrome://`) be excluded at parse time or in the UI? (Currently: stored; `javascript:` filtered in the UI.)

## 9. Working Rules Agreed

- Global navigation mode, not per source.
- Display URLs are trimmed for space only. Opening always uses the full URL.
- Folders first in tree results.
- Keep UI-independent logic in `Core/` so it can be unit tested without Command Palette.
- Verify Command Palette API names against the generated template before writing UI code.
- Verify local Chrome file layout before relying on the parser.
