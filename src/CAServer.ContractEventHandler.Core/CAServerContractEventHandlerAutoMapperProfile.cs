using System;
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
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
                    Id = e.GuardianInfo.VerificationInfo.Id.IsNullOrWhiteSpace()
                        ? Hash.Empty : Hash.LoadFromHex(e.GuardianInfo.VerificationInfo.Id),
                    Signature = e.GuardianInfo.VerificationInfo.Signature.IsNullOrWhiteSpace() 
                        ? ByteString.Empty : ByteStringHelper.FromHexString(e.GuardianInfo.VerificationInfo.Signature),
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc.IsNullOrWhiteSpace() 
                        ? string.Empty : e.GuardianInfo.VerificationInfo.VerificationDoc
                },
                ZkOidcInfo = new ZkJwtAuthInfo
                {
                    IdentifierHash = e.GuardianInfo.IdentifierHash.IsNullOrWhiteSpace()
                        ? Hash.Empty : Hash.LoadFromHex(e.GuardianInfo.IdentifierHash),
                    Salt = e.GuardianInfo.ZkJwtAuthInfo.Salt,
                    Nonce = e.GuardianInfo.ZkJwtAuthInfo.Nonce,
                    ZkProof = e.GuardianInfo.ZkJwtAuthInfo.ZkProof,
                    Issuer = e.GuardianInfo.ZkJwtAuthInfo.Issuer,
                    Kid = e.GuardianInfo.ZkJwtAuthInfo.Kid,
                    NoncePayload = new NoncePayload
                    {
                        AddManagerAddress = new AddManager
                        {
                            IdentifierHash = e.GuardianInfo.IdentifierHash.IsNullOrWhiteSpace()
                                ? Hash.Empty : Hash.LoadFromHex(e.GuardianInfo.IdentifierHash),
                            ManagerAddress = e.ManagerInfo.Address.IsNullOrWhiteSpace()
                                ? new Address() : Address.FromBase58(e.ManagerInfo.Address),
                            Timestamp = new Timestamp
                            {
                                Seconds = e.GuardianInfo.ZkJwtAuthInfo.NoncePayload.AddManager.Timestamp / 1000,
                                Nanos = (int)((e.GuardianInfo.ZkJwtAuthInfo.NoncePayload.AddManager.Timestamp % 1000) * 1000000)
                            }
                        }
                    }
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
                        Id = g.VerificationInfo.Id.IsNullOrWhiteSpace()
                            ? Hash.Empty : Hash.LoadFromHex(g.VerificationInfo.Id),
                        Signature = g.VerificationInfo.Signature.IsNullOrWhiteSpace() 
                            ? ByteString.Empty : ByteStringHelper.FromHexString(g.VerificationInfo.Signature),
                        VerificationDoc = g.VerificationInfo.VerificationDoc.IsNullOrWhiteSpace() 
                            ? string.Empty : g.VerificationInfo.VerificationDoc
                    },
                    ZkOidcInfo = new ZkJwtAuthInfo
                    {
                        IdentifierHash = g.IdentifierHash.IsNullOrWhiteSpace()
                            ? Hash.Empty : Hash.LoadFromHex(g.IdentifierHash),
                        Salt = g.ZkJwtAuthInfo.Salt.IsNullOrEmpty() ? string.Empty : g.ZkJwtAuthInfo.Salt,
                        Nonce = g.ZkJwtAuthInfo.Nonce.IsNullOrEmpty() ? string.Empty : g.ZkJwtAuthInfo.Nonce,
                        ZkProof = g.ZkJwtAuthInfo.ZkProof.IsNullOrEmpty() ? string.Empty : g.ZkJwtAuthInfo.Nonce,
                        Issuer = g.ZkJwtAuthInfo.Issuer.IsNullOrEmpty() ? string.Empty : g.ZkJwtAuthInfo.Issuer,
                        Kid = g.ZkJwtAuthInfo.Kid.IsNullOrEmpty() ? string.Empty : g.ZkJwtAuthInfo.Kid,
                        NoncePayload = new NoncePayload
                        {
                            AddManagerAddress = new AddManager
                            {
                                IdentifierHash = g.IdentifierHash.IsNullOrWhiteSpace()
                                    ? Hash.Empty : Hash.LoadFromHex(g.IdentifierHash),
                                ManagerAddress = e.ManagerInfo.Address.IsNullOrWhiteSpace()
                                    ? new Address() : Address.FromBase58(e.ManagerInfo.Address),
                                Timestamp = new Timestamp
                                {
                                    Seconds = g.ZkJwtAuthInfo.NoncePayload.AddManager.Timestamp / 1000,
                                    Nanos = (int)((g.ZkJwtAuthInfo.NoncePayload.AddManager.Timestamp % 1000) * 1000000)
                                }
                            }
                        }
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