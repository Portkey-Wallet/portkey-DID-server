using System.ComponentModel.DataAnnotations;

namespace CAServer.Bookmark.Dtos;

public class CreateBookmarkDto
{
    [Required] public string Name { get; set; }
    [Required] public string Url { get; set; }
}