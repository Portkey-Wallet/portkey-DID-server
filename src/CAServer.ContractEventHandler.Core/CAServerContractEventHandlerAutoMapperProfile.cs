using System.Linq;
using AElf;
using AElf.Types;
using AutoMapper;
using CAServer.Bookmark.Etos;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.DataReporting.Etos;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using Portkey.Contracts.CA;

namespace CAServer.ContractEventHandler.Core;

public class CAServerContractEventHandlerAutoMapperProfile : Profile
{
    public CAServerContractEventHandlerAutoMapperProfile()
    {
        CreateMap<AccountRegisterCreateEto, CreateHolderDto>()
            .ForMember(d => d.GuardianInfo, opt => opt.MapFrom(e => new GuardianInfo
            {
                Type = (GuardianType)(int)e.GuardianInfo.Type,
                IdentifierHash = Hash.LoadFromHex(e.GuardianInfo.IdentifierHash),
                VerificationInfo = new VerificationInfo
                {
                    Id = Hash.LoadFromHex(e.GuardianInfo.VerificationInfo.Id),
                    Signature = ByteStringHelper.FromHexString(e.GuardianInfo.VerificationInfo.Signature),
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc
                }
            }))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = Address.FromBase58(e.ManagerInfo.Address),
                ExtraData = e.ManagerInfo.ExtraData
            }));

        CreateMap<AccountRecoverCreateEto, SocialRecoveryDto>()
            .ForMember(d => d.GuardianApproved,
                opt => opt.MapFrom(e => e.GuardianApproved.Select(g => new GuardianInfo
                {
                    Type = (GuardianType)(int)g.Type,
                    IdentifierHash = Hash.LoadFromHex(g.IdentifierHash),
                    VerificationInfo = new VerificationInfo
                    {
                        Id = Hash.LoadFromHex(g.VerificationInfo.Id),
                        Signature = ByteStringHelper.FromHexString(g.VerificationInfo.Signature),
                        VerificationDoc = g.VerificationInfo.VerificationDoc
                    }
                }).ToList()))
            .ForMember(d => d.ManagerInfo, opt => opt.MapFrom(e => new ManagerInfo
            {
                Address = Address.FromBase58(e.ManagerInfo.Address),
                ExtraData = e.ManagerInfo.ExtraData
            }))
            .ForMember(d => d.LoginGuardianIdentifierHash,
                opt => opt.MapFrom(g => Hash.LoadFromHex(g.LoginGuardianIdentifierHash)));

        CreateMap<TransactionReportEto, TransactionReportContext>();
    }
}