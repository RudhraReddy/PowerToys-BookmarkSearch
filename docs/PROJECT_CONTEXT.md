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

- **Chrome is not installed** on the dev machine (no `Google\Chrome\User Data` folder).
- **Format is JSON**, not SQLite. Confirmed on two Chromium browsers:
  - Edge: `%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Bookmarks`. Roots: `bookmark_bar`, `other`, `synced`, plus an extra Edge-only `workspaces_v2`.
  - Brave: `%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\Default\Bookmarks`. Roots: `bookmark_bar`, `other`, `synced`.
- The parser should read only the three known roots and ignore unknown ones like `workspaces_v2`.
- Because settings take a list of file paths, Edge and Brave work with no extra code. Use them for local testing.

## 4. User-Facing Behavior

### 4.1 Trigger and Query Syntax
- Input starts with `/`. Anything not starting with `/` is not a bookmark query.
- `/` alone shows top-level folders (tree mode) or all bookmarks (flat mode).
- Trailing `/` browses folder contents (tree mode): `/anime/` shows contents of `anime`.
- Last segment is a search term matched against names.
- Example tree query: `/anime/an1` opens the matching bookmark.
- Example flat query: `/an1` finds every bookmark named like "an1", in any folder.
- **Open question:** whether a single `/` is accepted as a trigger keyword in CmdPal. If not, use a word trigger (e.g. `bm`) with `/` as the path separator. Verify before finalizing the shortcut table.

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
- One folder match: complete to full folder path with trailing `/`.
- Several folder matches: complete to their common prefix. Second Tab cycles choices.
- No folder matches: input unchanged.
- Tab does nothing in flat mode.

### 4.6 Ranking
- Score: exact (3) > prefix (2) > substring (1). No match is excluded.
- Matching is case-insensitive.
- Ties keep the source order (stable sort). The list should not reshuffle while typing.
- Tree mode: **folders listed before bookmarks** so Tab completion stays predictable.

### 4.7 Shortcut Prefixes (settings)
- User-configurable table of prefix → action.
- Planned examples:
  - `@` → run the rest of the input in PowerShell
  - `??` → web search with a URL template, `{query}` URL-encoded
- Check longer prefixes before shorter ones (`??` before `?`).
- Bookmark titles and URLs must never trigger a command.

### 4.8 Opening Items
- Bookmarks and web searches open in the default browser via `Process.Start` with `UseShellExecute = true`.

## 5. Code Layout (planned)

The Core code and tests below were written in an earlier session but are **not in this repo yet**.

```
BookmarkNav/
  Core/
    BookmarkEntry.cs          record + UrlDisplay.Trim
    ChromeBookmarkReader.cs   reads, copies, parses the Bookmarks JSON
    QueryParser.cs            "/" input -> ParsedQuery (folders, term, trailing slash)
    Matching.cs               Score(name, term)
    BookmarkSearch.cs         Search(index, input, mode), Complete(input, mode, results)
  Tests/
    BookmarkSearchTests.cs    xUnit tests for root, trailing slash, full path, flat mode, Tab
  UI/                         (not yet written) Command Palette page, settings page
```

Key types:
- `BookmarkEntry(Name, Url, FolderPath)`, with `FolderDisplay` and `DisplayUrl`.
- `SearchResult(Kind, Title, Subtitle, Url, FolderPath)`. `Kind` is `Folder` or `Bookmark`.
- `NavigationMode`: `Tree` | `Flat`.
- `ParsedQuery(Folders, Term, TrailingSlash)`.

## 6. Work Completed

1. Decided on Command Palette over PowerToys Run.
2. Designed query grammar, tree/flat modes, result layout, Tab behavior, display-URL rule.
3. Wrote `ChromeBookmarkReader` and `BookmarkEntry` (parser, root-name stripping, temp-copy read).
4. Wrote `QueryParser`, `Matching`, and `BookmarkSearch` (search and Tab completion).
5. Wrote xUnit tests for core search behavior.
6. Verified the bookmark file format locally (JSON) using Edge and Brave. See 3.1.

## 7. Next Steps

1. ~~Verify the local Chrome `Bookmarks` path and file format on the dev machine.~~ Done (see 3.1).
2. Create the Command Palette project from the template and confirm it builds and loads.
3. Add the Core code and xUnit tests to this repo and run them.
4. Write the Command Palette list page: call `BookmarkSearch.Search` on each keystroke, map `SearchResult` to list items, handle Tab via `Complete`, handle Enter via `Url` or `FolderPath`.
5. Write the settings page: navigation mode, bookmark file paths, shortcut prefixes.
6. Add `FileSystemWatcher` re-indexing.
7. Add shortcut prefix handling (`@` PowerShell, `??` web search).
8. Test with real profiles plus a fake file containing duplicate names, nested folders, and both `www.` and non-`www.` URLs.
9. Decide the trigger keyword (`/` vs word trigger).
10. Packaging and submission, if pursued (Store certification typically up to three business days; PowerToys Run plugin distribution is separate and not verified for CmdPal).

## 8. Open Questions

- Is `/` allowed as a CmdPal trigger keyword? (Decides query entry point.)
- Should flat mode narrow by typed folder segments, or always ignore them? (Currently: ignore.)
- Should non-web URLs (`javascript:`, `chrome://`) be excluded at parse time or in the UI? (Currently: stored, filter in UI.)
- Edge and Brave support: officially support and auto-detect in the first release, or later? (They already work by adding their file path in settings.)
- Shortcut table defaults beyond `@` and `??`?

## 9. Working Rules Agreed

- Global navigation mode, not per source.
- Display URLs are trimmed for space only. Opening always uses the full URL.
- Folders first in tree results.
- Keep UI-independent logic in `Core/` so it can be unit tested without Command Palette.
- Verify Command Palette API names against the generated template before writing UI code.
- Verify local Chrome file layout before relying on the parser.
