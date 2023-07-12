using System;

namespace CAServer.Bookmark.Dtos;

public class BookmarkResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public int GrainIndex { get; set; }
}