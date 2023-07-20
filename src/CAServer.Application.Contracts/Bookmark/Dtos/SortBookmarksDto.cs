using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Bookmark.Dtos;

public class SortBookmarksDto
{
    [Required] public List<BookmarkSortInfo> SortItems { get; set; }
}

public class BookmarkSortInfo
{
    public Guid Id { get; set; }
    public int SortWeight { get; set; }
}