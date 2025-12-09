namespace CAServer.Grains.Grain.Bookmark.Dtos;

[GenerateSerializer]
public class BookmarkGrainResultDto
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