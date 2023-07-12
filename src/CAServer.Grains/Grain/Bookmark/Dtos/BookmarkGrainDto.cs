namespace CAServer.Grains.Grain.Bookmark.Dtos;

public class BookmarkGrainDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
}