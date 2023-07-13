using System;
using Volo.Abp.EventBus;

namespace CAServer.Bookmark.Etos;

[EventName("BookmarkCreateEto")]
public class BookmarkCreateEto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public long ModificationTime { get; set; }
    public int Index { get; set; }
}