# Microsoft Store Submission

## 1. Reserve the app (Partner Center, once)

1. Sign in to [Partner Center](https://partner.microsoft.com/dashboard/home) and register as an individual developer if you haven't.
2. **Apps and games** > **New product** > **MSIX or PWA app**. Reserve a name, e.g. `Bookmark Search for Command Palette`.
3. Open **Product management** > **Product identity** and copy:
   - `Package/Identity/Name`
   - `Package/Identity/Publisher` (looks like `CN=XXXXXXXX-XXXX-...`)
   - `Package/Properties/PublisherDisplayName`

If `PublisherDisplayName` is not `RudhraReddy`, update `<PublisherDisplayName>` in `BookmarkSearch/Package.appxmanifest` to match.

## 2. Build the bundle

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-store.ps1 `
    -IdentityName "<Package/Identity/Name>" `
    -Publisher "<Package/Identity/Publisher>" `
    -Version 1.0.0.0
```

This writes the identity into `Package.appxmanifest` (commit it) and produces:

```
AppPackages\<IdentityName>_1.0.0.0_x64_arm64.msixbundle
```

The bundle is unsigned on purpose; the Store signs it.

## 3. Submission

**Packages:** upload the `.msixbundle`.

**Properties**
- Category: Productivity (subcategory: none), or Utilities + tools.
- Privacy policy URL: a public link to `PRIVACY.md` (see note below).
- Support contact: the GitHub issues URL.

**Store listing (English)**

Description:

> Bookmark Search adds your browser bookmarks to PowerToys Command Palette.
>
> Type `/` (after assigning the alias) to search Chrome, Edge and Brave bookmarks as you type, walk folders with `folder/name`, and press Enter to open a bookmark in your default browser.
>
> - Searches every profile of Chrome, Edge and Brave automatically
> - Tree mode (walk folders) or Flat mode (search every bookmark by name)
> - Updates live when your bookmarks change
> - Works offline; no data leaves your PC
>
> Requires Microsoft PowerToys with Command Palette enabled.

Short description: `Search your Chrome, Edge and Brave bookmarks from PowerToys Command Palette.`

Features:
- Search bookmarks from Command Palette
- Browse bookmark folders with folder/name
- Chrome, Edge and Brave, all profiles
- Live updates when bookmarks change

Keywords: `bookmarks`, `command palette`, `powertoys`, `chrome`, `edge`, `brave`, `search`

Screenshots: at least one, 1366×768 or larger, of Command Palette showing results.

**Notes for certification**

> This is an extension for Microsoft PowerToys Command Palette and has no window of its own.
> To test:
> 1. Install Microsoft PowerToys (https://github.com/microsoft/PowerToys or the Microsoft Store) and enable Command Palette.
> 2. Install this package.
> 3. Open Command Palette (Win+Alt+Space) and select "Bookmark Search".
> 4. Results list the bookmarks of any installed Chrome, Edge or Brave profile. If no browser is installed, the list is empty.

**Pricing and availability:** Free.

## Updating

1. Bump the version: `.\scripts\package-store.ps1 -Version 1.0.1.0`
2. Commit the manifest change.
3. New submission in Partner Center, upload the new bundle.

The last part of the version must stay `0` (Store requirement).

## Privacy policy URL

The Store needs a public URL. The repo is private, so either make it public and use
`https://github.com/RudhraReddy/PowerToys-BookmarkSearch/blob/main/PRIVACY.md`,
or publish `PRIVACY.md` somewhere public (e.g. a public gist).

## Discoverability

Store-only extensions don't appear in Command Palette's built-in extension browser. Users find them through the Store, a direct Store link, or
`ms-windows-store://assoc/?Tags=AppExtension-com.microsoft.commandpalette`.
