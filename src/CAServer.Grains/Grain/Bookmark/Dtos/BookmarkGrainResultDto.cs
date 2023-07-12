namespace CAServer.Grains.Grain.Bookmark.Dtos;

public class BookmarkGrainResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public long ModificationTime { get; set; }
    public int GrainIndex { get; set; }
}