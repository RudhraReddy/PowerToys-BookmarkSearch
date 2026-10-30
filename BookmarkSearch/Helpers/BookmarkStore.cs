using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using BookmarkSearch.Core;

namespace BookmarkSearch;

/// <summary>
/// Holds the current bookmark index and rebuilds it when a watched Bookmarks file changes.
/// </summary>
internal sealed partial class BookmarkStore : IDisposable
{
    private const int ReloadDelayMs = 500;

    private readonly Lock _lock = new();
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Timer _reloadTimer;
    private IReadOnlyList<string> _files = [];
    private BookmarkIndex _index = new();

    public BookmarkStore()
    {
        _reloadTimer = new Timer(_ => Reload());
    }

    public event EventHandler? Changed;

    public BookmarkIndex Index => Volatile.Read(ref _index);

    public void SetFiles(IReadOnlyList<string> files)
    {
        lock (_lock)
        {
            _files = files;
            Watch(files);
        }

        Reload();
    }

    public void Reload()
    {
        var index = new BookmarkIndex();
        lock (_lock)
        {
            foreach (var file in _files)
            {
                try
                {
                    ChromeBookmarkReader.ReadFile(file, index);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
                {
                    // Missing, locked or half-written file: skip it until the next change.
                }
            }
        }

        Volatile.Write(ref _index, index);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            DisposeWatchers();
        }

        _reloadTimer.Dispose();
    }

    private void Watch(IReadOnlyList<string> files)
    {
        DisposeWatchers();

        var directories = files
            .Select(Path.GetDirectoryName)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists);

        foreach (var directory in directories)
        {
            // Browsers save by writing a temp file and renaming it to "Bookmarks".
            var watcher = new FileSystemWatcher(directory, "Bookmarks")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            };
            watcher.Changed += (_, _) => ScheduleReload();
            watcher.Created += (_, _) => ScheduleReload();
            watcher.Renamed += (_, _) => ScheduleReload();
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
        }
    }

    private void ScheduleReload() => _reloadTimer.Change(ReloadDelayMs, Timeout.Infinite);

    private void DisposeWatchers()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
    }
}
