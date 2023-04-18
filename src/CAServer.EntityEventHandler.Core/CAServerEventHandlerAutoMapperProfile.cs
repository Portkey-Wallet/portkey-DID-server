using AutoMapper;
using CAServer.Entities.Es;
using CAServer.Entities.Etos;
using CAServer.Etos;
using CAServer.Etos.Chain;

namespace CAServer.EntityEventHandler.Core;

public class CAServerEventHandlerAutoMapperProfile : Profile
{
    public CAServerEventHandlerAutoMapperProfile()
    {
        CreateMap<ContactIndex, ContactCreateEto>().ReverseMap();
        CreateMap<ContactUpdateEto,ContactIndex>();
        CreateMap<ContactAddress, ContactAddressEto>().ReverseMap();
        CreateMap<AccountRegisterCompletedEto, AccountRegisterIndex>();
        CreateMap<AccountRegisterCreateEto, AccountRegisterIndex>();
        CreateMap<AccountRecoverCreateEto, AccountRecoverIndex>();
        CreateMap<AccountRecoverCompletedEto, AccountRecoverIndex>();
        CreateMap<UserTokenEto, UserTokenIndex>();
        CreateMap<CreateCAHolderEto, CAHolderIndex>();
        CreateMap<UpdateCAHolderEto, CAHolderIndex>();
        CreateMap<ChainCreateEto, ChainsInfoIndex>();
        CreateMap<ChainUpdateEto, ChainsInfoIndex>();
        CreateMap<ChainDeleteEto, ChainsInfoIndex>();
    }
}