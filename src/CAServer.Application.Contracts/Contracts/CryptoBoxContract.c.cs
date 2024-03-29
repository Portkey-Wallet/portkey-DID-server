// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: crypto_box_contract.proto
// </auto-generated>
// Original file comments:
// *
// CryptoBoxContract.proto
#pragma warning disable 0414, 1591
#region Designer generated code

using System.Collections.Generic;
using aelf = global::AElf.CSharp.Core;

namespace Portkey.Contracts.CryptoBox {

  #region Events
  public partial class CryptoBoxCreated : aelf::IEvent<CryptoBoxCreated>
  {
    public global::System.Collections.Generic.IEnumerable<CryptoBoxCreated> GetIndexed()
    {
      return new List<CryptoBoxCreated>
      {
      };
    }

    public CryptoBoxCreated GetNonIndexed()
    {
      return new CryptoBoxCreated
      {
        Sender = Sender,
        CryptoBoxId = CryptoBoxId,
        CryptoBoxSymbol = CryptoBoxSymbol,
        TotalAmount = TotalAmount,
        TotalCount = TotalCount,
        CryptoBoxType = CryptoBoxType,
        Receiver = Receiver,
      };
    }
  }

  public partial class CryptoBoxReceived : aelf::IEvent<CryptoBoxReceived>
  {
    public global::System.Collections.Generic.IEnumerable<CryptoBoxReceived> GetIndexed()
    {
      return new List<CryptoBoxReceived>
      {
      };
    }

    public CryptoBoxReceived GetNonIndexed()
    {
      return new CryptoBoxReceived
      {
        Sender = Sender,
        CryptoBoxId = CryptoBoxId,
        CryptoBoxSymbol = CryptoBoxSymbol,
        Amount = Amount,
        Receiver = Receiver,
        IsSuccess = IsSuccess,
      };
    }
  }

  public partial class CryptoBoxRefunded : aelf::IEvent<CryptoBoxRefunded>
  {
    public global::System.Collections.Generic.IEnumerable<CryptoBoxRefunded> GetIndexed()
    {
      return new List<CryptoBoxRefunded>
      {
      };
    }

    public CryptoBoxRefunded GetNonIndexed()
    {
      return new CryptoBoxRefunded
      {
        CryptoBoxId = CryptoBoxId,
        RefundAddress = RefundAddress,
        CryptoBoxSymbol = CryptoBoxSymbol,
        Amount = Amount,
      };
    }
  }

  public partial class AdminChanged : aelf::IEvent<AdminChanged>
  {
    public global::System.Collections.Generic.IEnumerable<AdminChanged> GetIndexed()
    {
      return new List<AdminChanged>
      {
      new AdminChanged
      {
        Address = Address
      },
      };
    }

    public AdminChanged GetNonIndexed()
    {
      return new AdminChanged
      {
      };
    }
  }

  #endregion
  public static partial class CryptoBoxContractContainer
  {
    static readonly string __ServiceName = "CryptoBoxContract";

    #region Marshallers
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.InitializeInput> __Marshaller_InitializeInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.InitializeInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Google.Protobuf.WellKnownTypes.Empty> __Marshaller_google_protobuf_Empty = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Google.Protobuf.WellKnownTypes.Empty.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.CreateCryptoBoxInput> __Marshaller_CreateCryptoBoxInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.CreateCryptoBoxInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.TransferCryptoBoxesInput> __Marshaller_TransferCryptoBoxesInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.TransferCryptoBoxesInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.GetCryptoBoxInput> __Marshaller_GetCryptoBoxInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.GetCryptoBoxInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.GetCryptoBoxOutput> __Marshaller_GetCryptoBoxOutput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.GetCryptoBoxOutput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.GetCryptoBoxMaxCountOutput> __Marshaller_GetCryptoBoxMaxCountOutput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.GetCryptoBoxMaxCountOutput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.SetCryptoBoxMaxCountInput> __Marshaller_SetCryptoBoxMaxCountInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.SetCryptoBoxMaxCountInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.RefundCryptoBoxInput> __Marshaller_RefundCryptoBoxInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.RefundCryptoBoxInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.AdminInput> __Marshaller_AdminInput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.AdminInput.Parser.ParseFrom);
    static readonly aelf::Marshaller<global::Portkey.Contracts.CryptoBox.AdminOutput> __Marshaller_AdminOutput = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Portkey.Contracts.CryptoBox.AdminOutput.Parser.ParseFrom);
    #endregion

    #region Methods
    static readonly aelf::Method<global::Portkey.Contracts.CryptoBox.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_Initialize = new aelf::Method<global::Portkey.Contracts.CryptoBox.InitializeInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "Initialize",
        __Marshaller_InitializeInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Portkey.Contracts.CryptoBox.CreateCryptoBoxInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_CreateCryptoBox = new aelf::Method<global::Portkey.Contracts.CryptoBox.CreateCryptoBoxInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "CreateCryptoBox",
        __Marshaller_CreateCryptoBoxInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Portkey.Contracts.CryptoBox.TransferCryptoBoxesInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_TransferCryptoBoxes = new aelf::Method<global::Portkey.Contracts.CryptoBox.TransferCryptoBoxesInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "TransferCryptoBoxes",
        __Marshaller_TransferCryptoBoxesInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Portkey.Contracts.CryptoBox.GetCryptoBoxInput, global::Portkey.Contracts.CryptoBox.GetCryptoBoxOutput> __Method_GetCryptoBoxInfo = new aelf::Method<global::Portkey.Contracts.CryptoBox.GetCryptoBoxInput, global::Portkey.Contracts.CryptoBox.GetCryptoBoxOutput>(
        aelf::MethodType.Action,
        __ServiceName,
        "GetCryptoBoxInfo",
        __Marshaller_GetCryptoBoxInput,
        __Marshaller_GetCryptoBoxOutput);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Portkey.Contracts.CryptoBox.GetCryptoBoxMaxCountOutput> __Method_GetCryptoBoxMaxCount = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Portkey.Contracts.CryptoBox.GetCryptoBoxMaxCountOutput>(
        aelf::MethodType.Action,
        __ServiceName,
        "GetCryptoBoxMaxCount",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_GetCryptoBoxMaxCountOutput);

    static readonly aelf::Method<global::Portkey.Contracts.CryptoBox.SetCryptoBoxMaxCountInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_SetCryptoBoxMaxCount = new aelf::Method<global::Portkey.Contracts.CryptoBox.SetCryptoBoxMaxCountInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "SetCryptoBoxMaxCount",
        __Marshaller_SetCryptoBoxMaxCountInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Portkey.Contracts.CryptoBox.RefundCryptoBoxInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_RefundCryptoBox = new aelf::Method<global::Portkey.Contracts.CryptoBox.RefundCryptoBoxInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "RefundCryptoBox",
        __Marshaller_RefundCryptoBoxInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Portkey.Contracts.CryptoBox.AdminInput, global::Google.Protobuf.WellKnownTypes.Empty> __Method_ChangeAdmin = new aelf::Method<global::Portkey.Contracts.CryptoBox.AdminInput, global::Google.Protobuf.WellKnownTypes.Empty>(
        aelf::MethodType.Action,
        __ServiceName,
        "ChangeAdmin",
        __Marshaller_AdminInput,
        __Marshaller_google_protobuf_Empty);

    static readonly aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Portkey.Contracts.CryptoBox.AdminOutput> __Method_GetAdmin = new aelf::Method<global::Google.Protobuf.WellKnownTypes.Empty, global::Portkey.Contracts.CryptoBox.AdminOutput>(
        aelf::MethodType.View,
        __ServiceName,
        "GetAdmin",
        __Marshaller_google_protobuf_Empty,
        __Marshaller_AdminOutput);

    #endregion

    #region Descriptors
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Portkey.Contracts.CryptoBox.CryptoBoxContractReflection.Descriptor.Services[0]; }
    }

    public static global::System.Collections.Generic.IReadOnlyList<global::Google.Protobuf.Reflection.ServiceDescriptor> Descriptors
    {
      get
      {
        return new global::System.Collections.Generic.List<global::Google.Protobuf.Reflection.ServiceDescriptor>()
        {
          global::AElf.Standards.ACS12.Acs12Reflection.Descriptor.Services[0],
          global::Portkey.Contracts.CryptoBox.CryptoBoxContractReflection.Descriptor.Services[0],
        };
      }
    }
    #endregion

    /// <summary>Base class for the contract of CryptoBoxContract</summary>
    // public abstract partial class CryptoBoxContractBase : AElf.Sdk.CSharp.CSharpSmartContract<Portkey.Contracts.CryptoBox.CryptoBoxContractState>
    // {
    //   public virtual global::Google.Protobuf.WellKnownTypes.Empty Initialize(global::Portkey.Contracts.CryptoBox.InitializeInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Google.Protobuf.WellKnownTypes.Empty CreateCryptoBox(global::Portkey.Contracts.CryptoBox.CreateCryptoBoxInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Google.Protobuf.WellKnownTypes.Empty TransferCryptoBoxes(global::Portkey.Contracts.CryptoBox.TransferCryptoBoxesInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Portkey.Contracts.CryptoBox.GetCryptoBoxOutput GetCryptoBoxInfo(global::Portkey.Contracts.CryptoBox.GetCryptoBoxInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Portkey.Contracts.CryptoBox.GetCryptoBoxMaxCountOutput GetCryptoBoxMaxCount(global::Google.Protobuf.WellKnownTypes.Empty input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Google.Protobuf.WellKnownTypes.Empty SetCryptoBoxMaxCount(global::Portkey.Contracts.CryptoBox.SetCryptoBoxMaxCountInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Google.Protobuf.WellKnownTypes.Empty RefundCryptoBox(global::Portkey.Contracts.CryptoBox.RefundCryptoBoxInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Google.Protobuf.WellKnownTypes.Empty ChangeAdmin(global::Portkey.Contracts.CryptoBox.AdminInput input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    //   public virtual global::Portkey.Contracts.CryptoBox.AdminOutput GetAdmin(global::Google.Protobuf.WellKnownTypes.Empty input)
    //   {
    //     throw new global::System.NotImplementedException();
    //   }
    //
    // }
    //
    // public static aelf::ServerServiceDefinition BindService(CryptoBoxContractBase serviceImpl)
    // {
    //   return aelf::ServerServiceDefinition.CreateBuilder()
    //       .AddDescriptors(Descriptors)
    //       .AddMethod(__Method_Initialize, serviceImpl.Initialize)
    //       .AddMethod(__Method_CreateCryptoBox, serviceImpl.CreateCryptoBox)
    //       .AddMethod(__Method_TransferCryptoBoxes, serviceImpl.TransferCryptoBoxes)
    //       .AddMethod(__Method_GetCryptoBoxInfo, serviceImpl.GetCryptoBoxInfo)
    //       .AddMethod(__Method_GetCryptoBoxMaxCount, serviceImpl.GetCryptoBoxMaxCount)
    //       .AddMethod(__Method_SetCryptoBoxMaxCount, serviceImpl.SetCryptoBoxMaxCount)
    //       .AddMethod(__Method_RefundCryptoBox, serviceImpl.RefundCryptoBox)
    //       .AddMethod(__Method_ChangeAdmin, serviceImpl.ChangeAdmin)
    //       .AddMethod(__Method_GetAdmin, serviceImpl.GetAdmin).Build();
    // }

  }
}
#endregion

