using System.Linq;
using System.Text;
using BookmarkSearch.Core;
using Xunit;

namespace BookmarkSearch.Tests;

public class BookmarkSearchTests
{
    // Duplicate names, nested folders, www and non-www URLs, an empty folder,
    // and an unknown root that must be ignored.
    private const string SampleJson = """
        {
          "roots": {
            "bookmark_bar": {
              "type": "folder", "name": "Bookmarks bar",
              "children": [
                { "type": "folder", "name": "anime", "children": [
                  { "type": "url", "name": "an1", "url": "https://www.an1.example.com/watch" },
                  { "type": "url", "name": "an10", "url": "https://an10.example.com/" },
                  { "type": "folder", "name": "seasonal", "children": [
                    { "type": "url", "name": "an1", "url": "http://seasonal.example.com/an1?x=1" }
                  ] }
                ] },
                { "type": "folder", "name": "animals", "children": [] },
                { "type": "url", "name": "GitHub", "url": "https://github.com/" }
              ]
            },
            "other": {
              "type": "folder", "name": "Other bookmarks",
              "children": [
                { "type": "url", "name": "Blog", "url": "https://blog.www.example.com/post" }
              ]
            },
            "synced": { "type": "folder", "name": "Mobile bookmarks", "children": [] },
            "workspaces_v2": {
              "type": "folder", "name": "Workspaces",
              "children": [ { "type": "url", "name": "Hidden", "url": "https://hidden.example.com/" } ]
            }
          }
        }
        """;

    private static BookmarkIndex LoadSample()
    {
        var index = new BookmarkIndex();
        ChromeBookmarkReader.Parse(Encoding.UTF8.GetBytes(SampleJson), index);
        return index;
    }

    private static string[] Titles(string input, NavigationMode mode) =>
        BookmarkSearcher.Search(LoadSample(), input, mode).Select(r => r.Title).ToArray();

    [Fact]
    public void Reader_SkipsRootNamesAndUnknownRoots()
    {
        var index = LoadSample();

        Assert.Equal(5, index.AllBookmarks.Count);
        Assert.DoesNotContain(index.AllBookmarks, b => b.Name == "Hidden");
        Assert.Equal("anime/seasonal", index.AllBookmarks.Single(b => b.Url.Contains("seasonal")).FolderDisplay);
        Assert.Equal(string.Empty, index.AllBookmarks.Single(b => b.Name == "Blog").FolderDisplay);
    }

    [Fact]
    public void Tree_RootShowsTopLevelFoldersFirst()
    {
        Assert.Equal(["anime", "animals", "GitHub", "Blog"], Titles("/", NavigationMode.Tree));
        Assert.Equal(Titles("/", NavigationMode.Tree), Titles(string.Empty, NavigationMode.Tree));
    }

    [Fact]
    public void Tree_TrailingSlashBrowsesFolder()
    {
        Assert.Equal(["seasonal", "an1", "an10"], Titles("/anime/", NavigationMode.Tree));
    }

    [Fact]
    public void Tree_FullPathRanksExactBeforePrefix()
    {
        var results = BookmarkSearcher.Search(LoadSample(), "/anime/an1", NavigationMode.Tree);

        Assert.Equal(["an1", "an10"], results.Select(r => r.Title));
        Assert.Equal("https://www.an1.example.com/watch", results[0].Url);
    }

    [Fact]
    public void Tree_FolderSegmentMatchesBestFolder()
    {
        Assert.Equal(["seasonal", "an1", "an10"], Titles("/anim/", NavigationMode.Tree));
        Assert.Empty(Titles("/nothing/", NavigationMode.Tree));
    }

    [Fact]
    public void Tree_TermMatchesFoldersAndBookmarks()
    {
        var results = BookmarkSearcher.Search(LoadSample(), "/ani", NavigationMode.Tree);

        Assert.Equal(["anime", "animals"], results.Select(r => r.Title));
        Assert.All(results, r => Assert.Equal(ResultKind.Folder, r.Kind));
        Assert.Equal("anime", results[0].Subtitle);
    }

    [Fact]
    public void Flat_SearchesAllBookmarksAndIgnoresFolders()
    {
        var results = BookmarkSearcher.Search(LoadSample(), "/an1", NavigationMode.Flat);

        Assert.Equal(["an1", "an1", "an10"], results.Select(r => r.Title));
        Assert.All(results, r => Assert.Equal(ResultKind.Bookmark, r.Kind));
        Assert.Equal(Titles("/an1", NavigationMode.Flat), Titles("/anime/an1", NavigationMode.Flat));
    }

    [Fact]
    public void Matching_IsCaseInsensitive()
    {
        Assert.Equal(3, Matching.Score("GitHub", "github"));
        Assert.Equal(2, Matching.Score("GitHub", "GIT"));
        Assert.Equal(1, Matching.Score("GitHub", "hub"));
        Assert.Equal(0, Matching.Score("GitHub", "lab"));
    }

    [Theory]
    [InlineData("https://www.example.com/path?q=1", "example.com/path?q=1")]
    [InlineData("http://example.com/", "example.com/")]
    [InlineData("https://blog.www.example.com/", "blog.www.example.com/")]
    [InlineData("HTTPS://WWW.Example.com", "Example.com")]
    [InlineData("chrome://settings", "chrome://settings")]
    public void UrlDisplay_TrimsSchemeAndLeadingWww(string url, string expected)
    {
        Assert.Equal(expected, UrlDisplay.Trim(url));
    }

    [Fact]
    public void Parse_SplitsFoldersAndTerm()
    {
        var query = ParsedQuery.Parse("/anime/seasonal/an1");

        Assert.Equal(["anime", "seasonal"], query.Folders);
        Assert.Equal("an1", query.Term);
    }

    [Fact]
    public void Tree_WorksWithoutLeadingSlash()
    {
        // The "/" alias opens the page with an empty box, so input has no leading "/".
        Assert.Equal(Titles("/anime/an1", NavigationMode.Tree), Titles("anime/an1", NavigationMode.Tree));
        Assert.Equal(Titles("/anime/", NavigationMode.Tree), Titles("anime/", NavigationMode.Tree));
    }
}
