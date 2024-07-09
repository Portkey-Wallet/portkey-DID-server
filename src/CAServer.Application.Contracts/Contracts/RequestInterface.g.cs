// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: request_interface.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace AetherLink.Contracts.Consumer {

  /// <summary>Holder for reflection information generated from request_interface.proto</summary>
  public static partial class RequestInterfaceReflection {

    #region Descriptor
    /// <summary>File descriptor for request_interface.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static RequestInterfaceReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChdyZXF1ZXN0X2ludGVyZmFjZS5wcm90bxIGb3JhY2xlGg9hZWxmL2NvcmUu",
            "cHJvdG8aEmFlbGYvb3B0aW9ucy5wcm90bxoLYWNzMTIucHJvdG8aG2dvb2ds",
            "ZS9wcm90b2J1Zi9lbXB0eS5wcm90bxoeZ29vZ2xlL3Byb3RvYnVmL3dyYXBw",
            "ZXJzLnByb3RvGh9nb29nbGUvcHJvdG9idWYvdGltZXN0YW1wLnByb3RvImUK",
            "F1N0YXJ0T3JhY2xlUmVxdWVzdElucHV0EhcKD3N1YnNjcmlwdGlvbl9pZBgB",
            "IAEoAxIaChJyZXF1ZXN0X3R5cGVfaW5kZXgYAiABKAUSFQoNc3BlY2lmaWNf",
            "ZGF0YRgDIAEoDCJ5ChxIYW5kbGVPcmFjbGVGdWxmaWxsbWVudElucHV0Eh4K",
            "CnJlcXVlc3RfaWQYASABKAsyCi5hZWxmLkhhc2gSEAoIcmVzcG9uc2UYAiAB",
            "KAwSCwoDZXJyGAMgASgMEhoKEnJlcXVlc3RfdHlwZV9pbmRleBgEIAEoBSIv",
            "Cg5PcmFjbGVSZXNwb25zZRIQCghyZXNwb25zZRgBIAEoDBILCgNlcnIYAiAB",
            "KAwyugEKEFJlcXVlc3RJbnRlcmZhY2USTQoSU3RhcnRPcmFjbGVSZXF1ZXN0",
            "Eh8ub3JhY2xlLlN0YXJ0T3JhY2xlUmVxdWVzdElucHV0GhYuZ29vZ2xlLnBy",
            "b3RvYnVmLkVtcHR5ElcKF0hhbmRsZU9yYWNsZUZ1bGZpbGxtZW50EiQub3Jh",
            "Y2xlLkhhbmRsZU9yYWNsZUZ1bGZpbGxtZW50SW5wdXQaFi5nb29nbGUucHJv",
            "dG9idWYuRW1wdHlCIKoCHUFldGhlckxpbmsuQ29udHJhY3RzLkNvbnN1bWVy",
            "YgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::AElf.Types.CoreReflection.Descriptor, global::AElf.OptionsReflection.Descriptor, global::AElf.Standards.ACS12.Acs12Reflection.Descriptor, global::Google.Protobuf.WellKnownTypes.EmptyReflection.Descriptor, global::Google.Protobuf.WellKnownTypes.WrappersReflection.Descriptor, global::Google.Protobuf.WellKnownTypes.TimestampReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::AetherLink.Contracts.Consumer.StartOracleRequestInput), global::AetherLink.Contracts.Consumer.StartOracleRequestInput.Parser, new[]{ "SubscriptionId", "RequestTypeIndex", "SpecificData" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::AetherLink.Contracts.Consumer.HandleOracleFulfillmentInput), global::AetherLink.Contracts.Consumer.HandleOracleFulfillmentInput.Parser, new[]{ "RequestId", "Response", "Err", "RequestTypeIndex" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::AetherLink.Contracts.Consumer.OracleResponse), global::AetherLink.Contracts.Consumer.OracleResponse.Parser, new[]{ "Response", "Err" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class StartOracleRequestInput : pb::IMessage<StartOracleRequestInput>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<StartOracleRequestInput> _parser = new pb::MessageParser<StartOracleRequestInput>(() => new StartOracleRequestInput());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<StartOracleRequestInput> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::AetherLink.Contracts.Consumer.RequestInterfaceReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StartOracleRequestInput() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StartOracleRequestInput(StartOracleRequestInput other) : this() {
      subscriptionId_ = other.subscriptionId_;
      requestTypeIndex_ = other.requestTypeIndex_;
      specificData_ = other.specificData_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StartOracleRequestInput Clone() {
      return new StartOracleRequestInput(this);
    }

    /// <summary>Field number for the "subscription_id" field.</summary>
    public const int SubscriptionIdFieldNumber = 1;
    private long subscriptionId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public long SubscriptionId {
      get { return subscriptionId_; }
      set {
        subscriptionId_ = value;
      }
    }

    /// <summary>Field number for the "request_type_index" field.</summary>
    public const int RequestTypeIndexFieldNumber = 2;
    private int requestTypeIndex_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int RequestTypeIndex {
      get { return requestTypeIndex_; }
      set {
        requestTypeIndex_ = value;
      }
    }

    /// <summary>Field number for the "specific_data" field.</summary>
    public const int SpecificDataFieldNumber = 3;
    private pb::ByteString specificData_ = pb::ByteString.Empty;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pb::ByteString SpecificData {
      get { return specificData_; }
      set {
        specificData_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as StartOracleRequestInput);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(StartOracleRequestInput other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (SubscriptionId != other.SubscriptionId) return false;
      if (RequestTypeIndex != other.RequestTypeIndex) return false;
      if (SpecificData != other.SpecificData) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (SubscriptionId != 0L) hash ^= SubscriptionId.GetHashCode();
      if (RequestTypeIndex != 0) hash ^= RequestTypeIndex.GetHashCode();
      if (SpecificData.Length != 0) hash ^= SpecificData.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (SubscriptionId != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(SubscriptionId);
      }
      if (RequestTypeIndex != 0) {
        output.WriteRawTag(16);
        output.WriteInt32(RequestTypeIndex);
      }
      if (SpecificData.Length != 0) {
        output.WriteRawTag(26);
        output.WriteBytes(SpecificData);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (SubscriptionId != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(SubscriptionId);
      }
      if (RequestTypeIndex != 0) {
        output.WriteRawTag(16);
        output.WriteInt32(RequestTypeIndex);
      }
      if (SpecificData.Length != 0) {
        output.WriteRawTag(26);
        output.WriteBytes(SpecificData);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (SubscriptionId != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(SubscriptionId);
      }
      if (RequestTypeIndex != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(RequestTypeIndex);
      }
      if (SpecificData.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeBytesSize(SpecificData);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(StartOracleRequestInput other) {
      if (other == null) {
        return;
      }
      if (other.SubscriptionId != 0L) {
        SubscriptionId = other.SubscriptionId;
      }
      if (other.RequestTypeIndex != 0) {
        RequestTypeIndex = other.RequestTypeIndex;
      }
      if (other.SpecificData.Length != 0) {
        SpecificData = other.SpecificData;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            SubscriptionId = input.ReadInt64();
            break;
          }
          case 16: {
            RequestTypeIndex = input.ReadInt32();
            break;
          }
          case 26: {
            SpecificData = input.ReadBytes();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 8: {
            SubscriptionId = input.ReadInt64();
            break;
          }
          case 16: {
            RequestTypeIndex = input.ReadInt32();
            break;
          }
          case 26: {
            SpecificData = input.ReadBytes();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class HandleOracleFulfillmentInput : pb::IMessage<HandleOracleFulfillmentInput>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<HandleOracleFulfillmentInput> _parser = new pb::MessageParser<HandleOracleFulfillmentInput>(() => new HandleOracleFulfillmentInput());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<HandleOracleFulfillmentInput> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::AetherLink.Contracts.Consumer.RequestInterfaceReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public HandleOracleFulfillmentInput() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public HandleOracleFulfillmentInput(HandleOracleFulfillmentInput other) : this() {
      requestId_ = other.requestId_ != null ? other.requestId_.Clone() : null;
      response_ = other.response_;
      err_ = other.err_;
      requestTypeIndex_ = other.requestTypeIndex_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public HandleOracleFulfillmentInput Clone() {
      return new HandleOracleFulfillmentInput(this);
    }

    /// <summary>Field number for the "request_id" field.</summary>
    public const int RequestIdFieldNumber = 1;
    private global::AElf.Types.Hash requestId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::AElf.Types.Hash RequestId {
      get { return requestId_; }
      set {
        requestId_ = value;
      }
    }

    /// <summary>Field number for the "response" field.</summary>
    public const int ResponseFieldNumber = 2;
    private pb::ByteString response_ = pb::ByteString.Empty;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pb::ByteString Response {
      get { return response_; }
      set {
        response_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "err" field.</summary>
    public const int ErrFieldNumber = 3;
    private pb::ByteString err_ = pb::ByteString.Empty;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pb::ByteString Err {
      get { return err_; }
      set {
        err_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "request_type_index" field.</summary>
    public const int RequestTypeIndexFieldNumber = 4;
    private int requestTypeIndex_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int RequestTypeIndex {
      get { return requestTypeIndex_; }
      set {
        requestTypeIndex_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as HandleOracleFulfillmentInput);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(HandleOracleFulfillmentInput other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(RequestId, other.RequestId)) return false;
      if (Response != other.Response) return false;
      if (Err != other.Err) return false;
      if (RequestTypeIndex != other.RequestTypeIndex) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (requestId_ != null) hash ^= RequestId.GetHashCode();
      if (Response.Length != 0) hash ^= Response.GetHashCode();
      if (Err.Length != 0) hash ^= Err.GetHashCode();
      if (RequestTypeIndex != 0) hash ^= RequestTypeIndex.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (requestId_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(RequestId);
      }
      if (Response.Length != 0) {
        output.WriteRawTag(18);
        output.WriteBytes(Response);
      }
      if (Err.Length != 0) {
        output.WriteRawTag(26);
        output.WriteBytes(Err);
      }
      if (RequestTypeIndex != 0) {
        output.WriteRawTag(32);
        output.WriteInt32(RequestTypeIndex);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (requestId_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(RequestId);
      }
      if (Response.Length != 0) {
        output.WriteRawTag(18);
        output.WriteBytes(Response);
      }
      if (Err.Length != 0) {
        output.WriteRawTag(26);
        output.WriteBytes(Err);
      }
      if (RequestTypeIndex != 0) {
        output.WriteRawTag(32);
        output.WriteInt32(RequestTypeIndex);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (requestId_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(RequestId);
      }
      if (Response.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeBytesSize(Response);
      }
      if (Err.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeBytesSize(Err);
      }
      if (RequestTypeIndex != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(RequestTypeIndex);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(HandleOracleFulfillmentInput other) {
      if (other == null) {
        return;
      }
      if (other.requestId_ != null) {
        if (requestId_ == null) {
          RequestId = new global::AElf.Types.Hash();
        }
        RequestId.MergeFrom(other.RequestId);
      }
      if (other.Response.Length != 0) {
        Response = other.Response;
      }
      if (other.Err.Length != 0) {
        Err = other.Err;
      }
      if (other.RequestTypeIndex != 0) {
        RequestTypeIndex = other.RequestTypeIndex;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (requestId_ == null) {
              RequestId = new global::AElf.Types.Hash();
            }
            input.ReadMessage(RequestId);
            break;
          }
          case 18: {
            Response = input.ReadBytes();
            break;
          }
          case 26: {
            Err = input.ReadBytes();
            break;
          }
          case 32: {
            RequestTypeIndex = input.ReadInt32();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            if (requestId_ == null) {
              RequestId = new global::AElf.Types.Hash();
            }
            input.ReadMessage(RequestId);
            break;
          }
          case 18: {
            Response = input.ReadBytes();
            break;
          }
          case 26: {
            Err = input.ReadBytes();
            break;
          }
          case 32: {
            RequestTypeIndex = input.ReadInt32();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class OracleResponse : pb::IMessage<OracleResponse>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<OracleResponse> _parser = new pb::MessageParser<OracleResponse>(() => new OracleResponse());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<OracleResponse> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::AetherLink.Contracts.Consumer.RequestInterfaceReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public OracleResponse() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public OracleResponse(OracleResponse other) : this() {
      response_ = other.response_;
      err_ = other.err_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public OracleResponse Clone() {
      return new OracleResponse(this);
    }

    /// <summary>Field number for the "response" field.</summary>
    public const int ResponseFieldNumber = 1;
    private pb::ByteString response_ = pb::ByteString.Empty;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pb::ByteString Response {
      get { return response_; }
      set {
        response_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "err" field.</summary>
    public const int ErrFieldNumber = 2;
    private pb::ByteString err_ = pb::ByteString.Empty;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pb::ByteString Err {
      get { return err_; }
      set {
        err_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as OracleResponse);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(OracleResponse other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Response != other.Response) return false;
      if (Err != other.Err) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (Response.Length != 0) hash ^= Response.GetHashCode();
      if (Err.Length != 0) hash ^= Err.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (Response.Length != 0) {
        output.WriteRawTag(10);
        output.WriteBytes(Response);
      }
      if (Err.Length != 0) {
        output.WriteRawTag(18);
        output.WriteBytes(Err);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (Response.Length != 0) {
        output.WriteRawTag(10);
        output.WriteBytes(Response);
      }
      if (Err.Length != 0) {
        output.WriteRawTag(18);
        output.WriteBytes(Err);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (Response.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeBytesSize(Response);
      }
      if (Err.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeBytesSize(Err);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(OracleResponse other) {
      if (other == null) {
        return;
      }
      if (other.Response.Length != 0) {
        Response = other.Response;
      }
      if (other.Err.Length != 0) {
        Err = other.Err;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Response = input.ReadBytes();
            break;
          }
          case 18: {
            Err = input.ReadBytes();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            Response = input.ReadBytes();
            break;
          }
          case 18: {
            Err = input.ReadBytes();
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code