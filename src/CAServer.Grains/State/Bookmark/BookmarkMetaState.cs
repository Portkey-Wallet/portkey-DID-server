namespace CAServer.Grains.State.Bookmark;

[GenerateSerializer]
public class BookmarkMetaState
{
    // BookmarkMetaState_userId
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public Guid UserId { get; set; }
	[Id(2)]
    public List<BookMarkMetaItem> Items { get; set; } = new();
}

[GenerateSerializer]
public class BookMarkMetaItem
{
	[Id(0)]
    public int Index { get; set; }
    
	[Id(1)]
    public int Size { get; set; }
    
}
