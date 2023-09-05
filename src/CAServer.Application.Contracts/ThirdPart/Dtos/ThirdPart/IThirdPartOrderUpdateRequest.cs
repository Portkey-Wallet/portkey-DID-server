using System;

namespace CAServer.ThirdPart.Dtos;

public interface IThirdPartValidOrderUpdateRequest
{
    public Guid Id { get; set; }
    public string Status { get; set; }
}

public interface IThirdPartNftOrderUpdateRequest
{
    
}