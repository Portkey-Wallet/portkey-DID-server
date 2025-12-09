using AutoMapper;
using CAServer.BackGround.Dtos;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Etos;

namespace CAServer.BackGround;

public class CAServerBackGroundAutoMapperProfile : Profile
{
    public CAServerBackGroundAutoMapperProfile()
    {
        CreateMap<TransactionEto, HandleTransactionDto>();
    }
}