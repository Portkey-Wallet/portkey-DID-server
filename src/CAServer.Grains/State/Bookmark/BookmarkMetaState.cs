namespace CAServer.Grains.State.Bookmark;

public class BookmarkMetaState
{
    // BookmarkMetaState_userId
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public List<BookMarkMetaItem> Items { get; set; } = new();
}

public class BookMarkMetaItem
{
    public int Index { get; set; }
    
    public int Size { get; set; }
    
}