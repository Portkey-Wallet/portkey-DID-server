using System;
using Orleans;

namespace CAServer.Bookmark.Dtos;

[GenerateSerializer]
public class BookmarkResultDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public string Name { get; set; }
    [Id(2)]
    public string Url { get; set; }
    [Id(3)]
    public int Index { get; set; }
}