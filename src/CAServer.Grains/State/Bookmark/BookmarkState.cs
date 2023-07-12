namespace CAServer.Grains.State.Bookmark;

public class BookmarkState
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public List<BookmarkItem> BookmarkItems { get; set; } = new();
}

public class BookmarkItem
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public long ModificationTime { get; set; }
    public int GrainIndex { get; set; }
}