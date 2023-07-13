using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Validation;

namespace CAServer.Bookmark.Dtos;

public class DeleteBookmarkDto
{
    [Required] public List<BookmarkId> Ids { get; set; }
}


public class BookmarkId
{
    [Required] public Guid Id { get; set; }
    [Required] public int GrainIndex { get; set; }
}