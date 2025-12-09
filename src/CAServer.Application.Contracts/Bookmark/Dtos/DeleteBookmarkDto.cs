using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Validation;

namespace CAServer.Bookmark.Dtos;

public class DeleteBookmarkDto
{
    [Required] public List<BookmarkInfo> DeleteInfos { get; set; }
}


public class BookmarkInfo
{
    [Required] public Guid Id { get; set; }
    [Required] public int Index { get; set; }
}