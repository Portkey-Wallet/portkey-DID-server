namespace CAServer.Image.Dto;

public class GetThumbnailInput
{
    public string ImageUrl { get; set; }
    
    public int Width{ get; set; }
    
    public int Height { get; set; }
}