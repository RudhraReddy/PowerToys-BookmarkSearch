using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace BookmarkSearch;

/// <summary>
/// Enter on a folder walks into it by replacing the search text with "/path/".
/// </summary>
internal sealed partial class BrowseFolderCommand : InvokableCommand
{
    private readonly BookmarkSearchPage _page;
    private readonly string _query;

    public BrowseFolderCommand(BookmarkSearchPage page, string query)
    {
        _page = page;
        _query = query;
        Name = "Browse folder";
    }

    public override ICommandResult Invoke()
    {
        _page.SetQuery(_query);
        return CommandResult.KeepOpen();
    }
}
