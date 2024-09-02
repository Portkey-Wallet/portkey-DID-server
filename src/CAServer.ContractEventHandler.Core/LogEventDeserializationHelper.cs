using System.Collections.Generic;
using System.Linq;
using AElf.Client.Dto;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using Google.Protobuf;

namespace AeFinder.Sdk;

public static class LogEventDeserializationHelper
{
    public static T DeserializeLogEvent<T>(LogEventDto logEvent) where T : IEvent<T>, new()
    {
        var nonIndexed = logEvent.NonIndexed;
        var indexedList = new List<string>(logEvent.Indexed);

        var @event = new AElf.Types.LogEvent
        {
            Indexed = { indexedList?.Select(ByteString.FromBase64) },
        };
        if (nonIndexed != null)
        {
            @event.NonIndexed = ByteString.FromBase64(nonIndexed);
        }

        var message = new T();
        message.MergeFrom(@event);
        return message;
    }
}