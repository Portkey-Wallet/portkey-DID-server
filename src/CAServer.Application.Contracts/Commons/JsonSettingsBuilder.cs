using System;
using AElf.Types;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Utilities.Encoders;

namespace CAServer.Commons;

public class JsonSettingsBuilder
{

    private readonly JsonSerializerSettings _instance = new();

    private JsonSettingsBuilder()
    {
    }

    public static JsonSettingsBuilder New()
    {
        return new JsonSettingsBuilder();
    }
    
    public JsonSerializerSettings Build()
    {
        return _instance;
    }


    public JsonSettingsBuilder WithCamelCasePropertyNamesResolver()
    {
        _instance.ContractResolver = new CamelCasePropertyNamesContractResolver();
        return this;
    }
    
    public JsonSettingsBuilder IgnoreNullValue()
    {
        _instance.NullValueHandling = NullValueHandling.Ignore;
        return this;
    }
    
    public JsonSettingsBuilder WithAElfTypesConverters()
    {
        return WithAElfAddressConverter()
            .WithByteStringBase64Converter()
            .WithHashHexConverter();
    }
    
    public JsonSettingsBuilder WithAElfAddressConverter()
    {
        _instance.Converters.Add(new AElfAddressConverter());
        return this;
    }
    
    public JsonSettingsBuilder WithHashHexConverter()
    {
        _instance.Converters.Add(new AElfHashHexConverter());
        return this;
    }

    public JsonSettingsBuilder WithHashBase64Converter()
    {
        _instance.Converters.Add(new AElfHashBase64Converter());
        return this;
    }

    public JsonSettingsBuilder WithByteStringBase64Converter()
    {
        _instance.Converters.Add(new ByteStringBase64Converter());
        return this;
    }

}


// AElf.Types.Address
public class AElfAddressConverter : JsonConverter<Address>
{
    public override void WriteJson(JsonWriter writer, Address value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToBase58());
    }

    public override Address ReadJson(JsonReader reader, Type objectType, Address existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        return reader?.Value == null ? null : Address.FromBase58(reader?.Value.ToString());
    }
}

// AElf.Types.Hash (Hex)
public class AElfHashHexConverter : JsonConverter<Hash>
{
    public override void WriteJson(JsonWriter writer, Hash value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToHex());
    }

    public override Hash ReadJson(JsonReader reader, Type objectType, Hash existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        return reader?.Value == null ? null : Hash.LoadFromHex(reader.Value.ToString());
    }
}

// AElf.Types.Hash (Base64)
public class AElfHashBase64Converter : JsonConverter<Hash>
{
    public override void WriteJson(JsonWriter writer, Hash value, JsonSerializer serializer)
    {
        writer.WriteValue(Base64.ToBase64String(value.ToByteArray()));
    }

    public override Hash ReadJson(JsonReader reader, Type objectType, Hash existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        return reader?.Value == null ? null : Hash.LoadFromBase64(reader.Value.ToString());
    }
}

// Google.Protobuf.ByteString (Base64)
public class ByteStringBase64Converter : JsonConverter<ByteString>
{
    public override void WriteJson(JsonWriter writer, ByteString value, JsonSerializer serializer)
    {
        writer.WriteValue(Base64.ToBase64String(value.ToByteArray()));
    }

    public override ByteString ReadJson(JsonReader reader, Type objectType, ByteString existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        return reader?.Value == null ? null : ByteString.FromBase64(reader.Value.ToString());
    }
}

