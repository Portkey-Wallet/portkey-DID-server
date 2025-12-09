namespace CAServer.Grains.State.Bookmark;

[GenerateSerializer]
public class BookmarkState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public Guid UserId { get; set; }
	[Id(2)]
    public List<BookmarkItem> BookmarkItems { get; set; } = new();
}

[GenerateSerializer]
public class BookmarkItem
{
	[Id(0)]
    public Guid Id { get; set; }
	[Id(1)]
    public string Name { get; set; }
	[Id(2)]
    public string Url { get; set; }
	[Id(3)]
    public long ModificationTime { get; set; }
	[Id(4)]
    public int Index { get; set; }
}
