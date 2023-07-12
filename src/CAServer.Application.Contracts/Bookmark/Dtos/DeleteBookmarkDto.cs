using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Validation;

namespace CAServer.Bookmark.Dtos;

public class DeleteBookmarkDto
{
    [Required] public Dictionary<Guid, int> Ids { get; set; }
}