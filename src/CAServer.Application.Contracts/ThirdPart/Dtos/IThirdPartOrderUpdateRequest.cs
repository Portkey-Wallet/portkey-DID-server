using System;

namespace CAServer.ThirdPart.Dtos;

public interface IThirdPartOrderUpdateRequest
{
    public Guid Id { get; set; }
    public string Status { get; set; }
}

public interface IThirdPartNftOrderUpdateRequest : IThirdPartOrderUpdateRequest
{
    
}