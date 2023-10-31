

using System;
using System.Threading;
using Serilog;

namespace CAServer;

public static class DashExecutionContext
{
     private static AsyncLocal<string> _traceIdentifier = new AsyncLocal<string>();

     public static string? TraceIdentifier => _traceIdentifier.Value;

    
     // public static bool TrySetTraceIdentifier(string traceIdentifier, bool force = false)
     // {
     //      return TrySetValue(nameof(TraceIdentifier), traceIdentifier, _traceIdentifier, string.IsNullOrEmpty, force);
     // }
     //
     // private static bool TrySetValue(
     //      string contextPropertyName,
     //      T newValue,
     //      AsyncLocal<string> ambientHolder,
     //      Func<> valueInvalidator,
     //      bool force)
     //      where T : IEquatable
     // {
     //      if (newValue is null || newValue.Equals(default) || valueInvalidator.Invoke(newValue))
     //      {
     //           return false;
     //      }
     //
     //      var currentValue = ambientHolder.Value;
     //      if (force || currentValue is null || currentValue.Equals(default) || valueInvalidator.Invoke(currentValue))
     //      {
     //           ambientHolder.Value = newValue;
     //           return true;
     //      }
     //      else if (!currentValue.Equals(newValue))
     //      {
     //           Log.Error($"Tried to set different value for {contextPropertyName}, but it is already set for this execution flow - " +
     //                     $"please, check the execution context logic! Current value: {currentValue} ; rejected value: {newValue}");
     //      }
     //
     //      return false;
     // }
    
}



