using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace CAServer;

public class RealIpOptionsValidator : IValidateOptions<RealIpOptions>
{
    public ValidateOptionsResult Validate(string name, RealIpOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("RealIp options are required.");
        }

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.HeaderKey))
        {
            failures.Add("RealIp:HeaderKey must not be empty.");
        }
        else if (!string.Equals(options.HeaderKey, options.HeaderKey.Trim(), System.StringComparison.Ordinal))
        {
            failures.Add("RealIp:HeaderKey must not contain leading or trailing whitespace.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
