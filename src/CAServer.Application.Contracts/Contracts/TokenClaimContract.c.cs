// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: token_claim_contract.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace Portkey.Contracts.TokenClaim {

  #region Events
  public partial class TokenClaimed : aelf::IEvent<TokenClaimed>
  {
    public global::System.Collections.Generic.IEnumerable<TokenClaimed> GetIndexed()
    {
      return new List<TokenClaimed>
      {
      };
    }

    public TokenClaimed GetNonIndexed()
    {
      return new TokenClaimed
      {
        User = User,
        Symbol = Symbol,
        Amount = Amount,
      };
    }
  }

  #endregion
  public static partial class TokenClaimContractContainer
  {
    static readonly string __ServiceName = "token_claim.TokenClaimContract";

    #region Marshallers
    static readonly aelf::Marshaller<global::Portkey.Contracts.TokenClaim.InitializeInput> __Marshaller_token_claim_InitializeInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.TokenClaim.InitializeInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.Empty> __Marshaller_google_protobuf_Empty = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.Empty.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.TokenClaim.ClaimTokenInput> __Marshaller_token_claim_ClaimTokenInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.TokenClaim.ClaimTokenInput.Parser.ParseFrom);
    #endregion

    #region Methods
    static readonly aelf::Method<global::Portkey.Contracts.TokenClaim.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Initialize = new aelf::Method<global::Portkey.Contracts.TokenClaim.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Initialize",
        __Marshaller_token_claim_InitializeInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Portkey.Contracts.TokenClaim.ClaimTokenInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_ClaimToken = new aelf::Method<global::Portkey.Contracts.TokenClaim.ClaimTokenInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "ClaimToken",
        __Marshaller_token_claim_ClaimTokenInput,
        __Marshaller_google_protobuf_Empty);

    #endregion

    #region Descriptors
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Portkey.Contracts.TokenClaim.TokenClaimContractReflection.Descriptor.Services[0]; }
    }

    public static global::System.Collections.Generic.IReadOnlyList<global::Google.Protobuf.Reflection.ServiceDescriptor> Descriptors
    {
      get
      {
        return new global::System.Collections.Generic.List<global::Google.Protobuf.Reflection.ServiceDescriptor>()
        {
          global::Portkey.Contracts.TokenClaim.TokenClaimContractReflection.Descriptor.Services[0],
        };
      }
    }
    #endregion

    /// <summary>Base class for the contract of TokenClaimContract</summary>
    // public abstract partial class TokenClaimContractBase : AElf.Sdk.CSharp.CSharpSmartContract<Portkey.Contracts.TokenClaim.TokenClaimContractState>
    // {
    //   public virtual global::Google.Protobuf.WellKnownTypes.Empty Initialize(global::Portkey.Contracts.TokenClaim.InitializeInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Google.Protobuf.WellKnownTypes.Empty ClaimToken(global::Portkey.Contracts.TokenClaim.ClaimTokenInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    // }

    // public static aelf::ServerServiceDefinition BindService(TokenClaimContractBase serviceImpl)
    // {
    //   return aelf::ServerServiceDefinition.CreateBuilder()
    //       .AddDescriptors(Descriptors)
    //       .AddMethod(__Method_Initialize, serviceImpl.Initialize)
    //       .AddMethod(__Method_ClaimToken, serviceImpl.ClaimToken).Build();
    // }

  }
}
#endregion

