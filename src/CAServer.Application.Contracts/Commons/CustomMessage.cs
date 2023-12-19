using System;

namespace CAServer.Commons;

public class CustomMessage<T>
{
    public string Image { get; set; }
    public string Link { get; set; }
    public T Data { get; set; }
}

public class RedPackageCard
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string Memo { get; set; }
}