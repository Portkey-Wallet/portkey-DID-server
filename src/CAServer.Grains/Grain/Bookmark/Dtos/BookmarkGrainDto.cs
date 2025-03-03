namespace CAServer.Grains.Grain.Bookmark.Dtos;

[GenerateSerializer]
public class BookmarkGrainDto
{
    [Id(0)]
    public Guid UserId { get; set; }
    
    [Id(1)]
    public string Name { get; set; }
    
    [Id(2)]
    public string Url { get; set; }
    
    [Id(3)]
    public int Index { get; set; }
    
}