using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Image.Dto;

public class GetThumbnailInput
{
    [Required] public string ImageUrl { get; set; }
    
    [Required] [Range(1,int.MaxValue)]public int Width{ get; set; }
    
    [Required] [Range(1,int.MaxValue)]public int Height { get; set; }
}