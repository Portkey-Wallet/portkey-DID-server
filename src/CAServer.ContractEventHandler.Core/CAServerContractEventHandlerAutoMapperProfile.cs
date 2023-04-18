using System.Linq;
using AElf;
using AElf.Types;
using AutoMapper;
using CAServer.Etos;
using CAServer.Grains.Grain.ApplicationHandler;
using GuardianType = CAServer.Account.GuardianType;

namespace CAServer.ContractEventHandler.Core;

public class CAServerContractEventHandlerAutoMapperProfile : Profile
{
    public CAServerContractEventHandlerAutoMapperProfile()
    {
        CreateMap<AccountRegisterCreateEto, CreateHolderDto>()
            .ForMember(d => d.GuardianAccountInfo, opt => opt.MapFrom(e => new GuardianAccountInfo
            {
                Type = e.GuardianAccountInfo.Type == GuardianType.GUARDIAN_TYPE_OF_EMAIL
                    ? Grains.Grain.ApplicationHandler.GuardianType.OfEmail
                    : Grains.Grain.ApplicationHandler.GuardianType.OfPhone,
                Value = e.GuardianAccountInfo.Value,
                VerificationInfo = new VerificationInfo
                {
                    Id = Hash.LoadFromHex(e.GuardianAccountInfo.VerificationInfo.Id),
                    Signature = ByteStringHelper.FromHexString(e.GuardianAccountInfo.VerificationInfo.Signature),
                    VerificationDoc = e.GuardianAccountInfo.VerificationInfo.VerificationDoc
                }
            }))
            .ForMember(d => d.Manager, opt => opt.MapFrom(e => new Manager
            {
                ManagerAddress = Address.FromBase58(e.Manager.ManagerAddress),
                DeviceString = e.Manager.DeviceString
            }));

        CreateMap<AccountRecoverCreateEto, SocialRecoveryDto>()
            .ForMember(d => d.GuardianApproved,
                opt => opt.MapFrom(e => e.GuardianApproved.Select(g => new GuardianAccountInfo
                {
                    Type = g.Type == GuardianType.GUARDIAN_TYPE_OF_EMAIL
                        ? Grains.Grain.ApplicationHandler.GuardianType.OfEmail
                        : Grains.Grain.ApplicationHandler.GuardianType.OfPhone,
                    Value = g.Value,
                    VerificationInfo = new VerificationInfo
                    {
                        Id = Hash.LoadFromHex(g.VerificationInfo.Id),
                        Signature = ByteStringHelper.FromHexString(g.VerificationInfo.Signature),
                        VerificationDoc = g.VerificationInfo.VerificationDoc
                    }
                }).ToList()))
            .ForMember(d => d.Manager, opt => opt.MapFrom(e => new Manager
            {
                ManagerAddress = Address.FromBase58(e.Manager.ManagerAddress),
                DeviceString = e.Manager.DeviceString
            }));
    }
}