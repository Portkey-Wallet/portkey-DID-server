using AutoMapper;
using CAServer.Entities.Es;
using MockServer.Dtos;

namespace MockServer;

public class CAServerMockServerAutoMapperProfile : Profile
{
    public CAServerMockServerAutoMapperProfile()
    {
        CreateMap<AlchemyOrderDto, AlchemyOrderIndex>();
        CreateMap<CreateAlchemyMockOrderDto, AlchemyOrderDto>();
        CreateMap<AlchemyOrderIndex, AlchemyOrderDto>();
        CreateMap<UpdateAlchemyMockOrderDto, AlchemyOrderDto>();
    }
}