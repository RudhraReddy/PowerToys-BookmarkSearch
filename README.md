# PowerToys Bookmark Search

A PowerToys Command Palette extension that searches your Chrome, Edge and Brave bookmarks.

## Usage

Open Command Palette (`Win+Alt+Space`) and type `/` to jump straight into Bookmark Search (see [Set up the `/` trigger](#set-up-the--trigger)).

| Input | Result |
|---|---|
| (empty) | Top-level folders and bookmarks |
| `anime/` | Contents of the `anime` folder |
| `anime/an1` | Items in `anime` matching `an1` |

- **Enter** on a bookmark opens it in the default browser. **Enter** on a folder walks into it.
- **Tab** on a folder completes its path.
- In **Flat** mode, folders are ignored and every bookmark is searched by name.

## Set up the `/` trigger

Extensions can't register a trigger themselves, so assign it once:

1. Open Command Palette settings > **Extensions** > **Bookmark Search**.
2. Select the **Bookmark Search** command, set **Alias** to `/`, and turn on **Direct** (activate without a space).

## Settings

Open from the **Bookmark Search** context menu in Command Palette.

- **Navigation mode:** Tree or Flat.
- **Bookmark files:** one path per line. Leave empty to find Chrome, Edge and Brave profiles automatically.

## Build

Requires the .NET 10 SDK and the Windows SDK 10.0.26100.

```powershell
dotnet test BookmarkSearch.Tests
```

To install locally, turn on Developer Mode (Settings > System > For developers), then:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\deploy.ps1
```

Then run **Reload** in Command Palette.

## Packaging

Store bundle (x64 + ARM64):

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-store.ps1
```

See [docs/STORE_SUBMISSION.md](docs/STORE_SUBMISSION.md) for the full submission steps.

## Icons

`BookmarkSearch/Assets/Icon.light.svg` and `Icon.dark.svg` are the Command Palette icons (light and dark theme). The package PNGs are generated from `Icon.light.svg`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\render-icons.ps1
```

## Privacy

Bookmark Search works offline and collects no data. See [PRIVACY.md](PRIVACY.md).

## License

[MIT](LICENSE)
