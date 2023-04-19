using AutoMapper;
using CAServer.Contacts;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.CrossChain;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Grains.State;
using CAServer.Grains.State.Chain;
using CAServer.Grains.State.Contacts;
using CAServer.Grains.State.CrossChain;
using CAServer.Grains.State.Tokens;

namespace CAServer.Grains;

public class CAServerGrainsAutoMapperProfile : Profile
{
    public CAServerGrainsAutoMapperProfile()
    {
        CreateMap<RegisterInfo, RegisterGrainDto>().ReverseMap();
        CreateMap<RecoveryInfo, RecoveryGrainDto>().ReverseMap();
        CreateMap<CAHolderState, CAHolderGrainDto>().ReverseMap();
        CreateMap<ChainState, ChainGrainDto>().ReverseMap();
        CreateMap<ContactAddress, ContactAddressDto>().ReverseMap();
        CreateMap<ContactState, ContactGrainDto>().ForMember(c => c.ModificationTime,
            d => d.MapFrom(s => new DateTimeOffset(s.ModificationTime).ToUnixTimeMilliseconds()));
        CreateMap<UserTokenState, UserTokenGrainDto>();
        CreateMap<CrossChainTransfer, CrossChainTransferDto>();
        CreateMap<CrossChainTransferDto, CrossChainTransfer>();
    }
}