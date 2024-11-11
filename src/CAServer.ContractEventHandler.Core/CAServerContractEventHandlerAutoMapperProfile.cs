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
using CAServer.FreeMint.Dtos;
using CAServer.Grains.Grain.ApplicationHandler;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Portkey.Contracts.CA;
using Enum = System.Enum;

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
                        ? Hash.Empty
                        : Hash.LoadFromHex(e.GuardianInfo.VerificationInfo.Id),
                    Signature = e.GuardianInfo.VerificationInfo.Signature.IsNullOrWhiteSpace()
                        ? ByteString.Empty
                        : ByteStringHelper.FromHexString(e.GuardianInfo.VerificationInfo.Signature),
                    VerificationDoc = e.GuardianInfo.VerificationInfo.VerificationDoc.IsNullOrWhiteSpace()
                        ? string.Empty
                        : e.GuardianInfo.VerificationInfo.VerificationDoc
                },
                ZkLoginInfo = e.GuardianInfo.ZkLoginInfo == null
                    ? new ZkLoginInfo()
                    : new ZkLoginInfo
                    {
                        IdentifierHash = e.GuardianInfo.ZkLoginInfo.IdentifierHash.IsNullOrWhiteSpace()
                            ? Hash.Empty
                            : Hash.LoadFromHex(e.GuardianInfo.ZkLoginInfo.IdentifierHash),
                        Salt = e.GuardianInfo.ZkLoginInfo.Salt.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.Salt,
                        Nonce = e.GuardianInfo.ZkLoginInfo.Nonce.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.Nonce,
                        ZkProof = e.GuardianInfo.ZkLoginInfo.ZkProof.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.ZkProof,
                        ZkProofInfo = e.GuardianInfo.ZkLoginInfo == null
                            ? new ZkProofInfo()
                            : new ZkProofInfo
                            {
                                ZkProofPiA = { e.GuardianInfo.ZkLoginInfo.ZkProofPiA },
                                ZkProofPiB1 = { e.GuardianInfo.ZkLoginInfo.ZkProofPiB1 },
                                ZkProofPiB2 = { e.GuardianInfo.ZkLoginInfo.ZkProofPiB2 },
                                ZkProofPiB3 = { e.GuardianInfo.ZkLoginInfo.ZkProofPiB3 },
                                ZkProofPiC = { e.GuardianInfo.ZkLoginInfo.ZkProofPiC }
                            },
                        Issuer = e.GuardianInfo.ZkLoginInfo.Issuer.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.Issuer,
                        Kid = e.GuardianInfo.ZkLoginInfo.Kid.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.Kid,
                        CircuitId = e.GuardianInfo.ZkLoginInfo.CircuitId.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.CircuitId,
                        PoseidonIdentifierHash = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                            ? string.Empty
                            : e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash,
                        IdentifierHashType = e.GuardianInfo.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                            ? IdentifierHashType.Sha256Hash
                            : IdentifierHashType.PoseidonHash,
                        NoncePayload = new NoncePayload
                        {
                            AddManagerAddress = new AddManager
                            {
                                CaHash = e.CaHash.IsNullOrWhiteSpace()
                                    ? Hash.Empty
                                    : Hash.LoadFromHex(e.CaHash),
                                ManagerAddress = e.ManagerInfo.Address.IsNullOrWhiteSpace()
                                    ? new Address()
                                    : Address.FromBase58(e.ManagerInfo.Address),
                                Timestamp = new Timestamp
                                {
                                    Seconds = e.GuardianInfo.ZkLoginInfo.NoncePayload.AddManager.Timestamp,
                                    Nanos = 0
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
            .ForMember(d => d.Platform,
            opt => opt.MapFrom(e => Enum.IsDefined(typeof(Platform), (int)e.Source) ? (Platform)(int)e.Source : Platform.Undefined))
            .ForMember(d => d.GuardianApproved,
                opt => opt.MapFrom(e => e.GuardianApproved.Select(g => new GuardianInfo
                {
                    Type = (GuardianType)(int)g.Type,
                    IdentifierHash = Hash.LoadFromHex(g.IdentifierHash),
                    VerificationInfo = new VerificationInfo
                    {
                        Id = g.VerificationInfo.Id.IsNullOrWhiteSpace()
                            ? Hash.Empty
                            : Hash.LoadFromHex(g.VerificationInfo.Id),
                        Signature = g.VerificationInfo.Signature.IsNullOrWhiteSpace()
                            ? ByteString.Empty
                            : ByteStringHelper.FromHexString(g.VerificationInfo.Signature),
                        VerificationDoc = g.VerificationInfo.VerificationDoc.IsNullOrWhiteSpace()
                            ? string.Empty
                            : g.VerificationInfo.VerificationDoc
                    },
                    ZkLoginInfo = new ZkLoginInfo
                    {
                        IdentifierHash = g.ZkLoginInfo.IdentifierHash.IsNullOrWhiteSpace()
                            ? Hash.Empty
                            : Hash.LoadFromHex(g.ZkLoginInfo.IdentifierHash),
                        Salt = g.ZkLoginInfo.Salt.IsNullOrEmpty() ? string.Empty : g.ZkLoginInfo.Salt,
                        Nonce = g.ZkLoginInfo.Nonce.IsNullOrEmpty() ? string.Empty : g.ZkLoginInfo.Nonce,
                        ZkProof = g.ZkLoginInfo.ZkProof.IsNullOrEmpty() ? string.Empty : g.ZkLoginInfo.ZkProof,
                        ZkProofInfo = g.ZkLoginInfo == null
                            ? new ZkProofInfo()
                            : new ZkProofInfo
                            {
                                ZkProofPiA = { g.ZkLoginInfo.ZkProofPiA },
                                ZkProofPiB1 = { g.ZkLoginInfo.ZkProofPiB1 },
                                ZkProofPiB2 = { g.ZkLoginInfo.ZkProofPiB2 },
                                ZkProofPiB3 = { g.ZkLoginInfo.ZkProofPiB3 },
                                ZkProofPiC = { g.ZkLoginInfo.ZkProofPiC }
                            },
                        Issuer = g.ZkLoginInfo.Issuer.IsNullOrEmpty() ? string.Empty : g.ZkLoginInfo.Issuer,
                        Kid = g.ZkLoginInfo.Kid.IsNullOrEmpty() ? string.Empty : g.ZkLoginInfo.Kid,
                        CircuitId = g.ZkLoginInfo.CircuitId.IsNullOrEmpty() ? string.Empty : g.ZkLoginInfo.CircuitId,
                        PoseidonIdentifierHash = g.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                            ? string.Empty
                            : g.ZkLoginInfo.PoseidonIdentifierHash,
                        IdentifierHashType = g.ZkLoginInfo.PoseidonIdentifierHash.IsNullOrEmpty()
                            ? IdentifierHashType.Sha256Hash
                            : IdentifierHashType.PoseidonHash,
                        NoncePayload = new NoncePayload
                        {
                            AddManagerAddress = new AddManager
                            {
                                CaHash = e.CaHash.IsNullOrWhiteSpace()
                                    ? Hash.Empty
                                    : Hash.LoadFromHex(e.CaHash),
                                ManagerAddress = e.ManagerInfo.Address.IsNullOrWhiteSpace()
                                    ? new Address()
                                    : Address.FromBase58(e.ManagerInfo.Address),
                                Timestamp = new Timestamp
                                {
                                    Seconds = g.ZkLoginInfo.NoncePayload.AddManager.Timestamp,
                                    Nanos = 0
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
        CreateMap<FreeMintCollectionInfo, CollectionInfo>();
        CreateMap<ItemMintInfo, FreeMintIndex>();
    }
}