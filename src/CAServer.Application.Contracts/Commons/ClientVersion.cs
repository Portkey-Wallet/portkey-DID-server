using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CAServer.Commons;

public class ClientVersion : IComparable<ClientVersion>
{
    private const string Pattern = @"^([vV])?([0-9]+\.)*[0-9]+$";
    private readonly int[] _version;

    public ClientVersion(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
        {
            throw new ArgumentException("Version string cannot be null or whitespace.");
        }

        if (!Regex.IsMatch(versionString, Pattern))
        {
            throw new ArgumentException("Invalid version format.");
        }
        _version = versionString.TrimStart('v', 'V')
            .Split('.')
            .Select(int.Parse)
            .ToArray();
    }

    public static ClientVersion Of(string versionString)
    {
        return versionString.IsNullOrEmpty() ? null : new ClientVersion(versionString);
    }
    
    public int CompareTo(ClientVersion other)
    {
        if (other == null) return 1;

        var length = Math.Max(_version.Length, other._version.Length);
        for (var i = 0; i < length; i++)
        {
            var thisPart = i < _version.Length ? _version[i] : 0;
            var otherPart = i < other._version.Length ? other._version[i] : 0;

            if (thisPart > otherPart) return 1;
            if (thisPart < otherPart) return -1;
        }

        return 0;
    }

    public static bool operator >(ClientVersion v1, ClientVersion v2)
    {
        return v1.CompareTo(v2) > 0;
    }

    public static bool operator <(ClientVersion v1, ClientVersion v2)
    {
        return v1.CompareTo(v2) < 0;
    }

    public static bool operator ==(ClientVersion v1, ClientVersion v2)
    {
        if (ReferenceEquals(v1, v2)) return true;
        if (ReferenceEquals(v1, null)) return false;
        if (ReferenceEquals(v2, null)) return false;

        return v1.CompareTo(v2) == 0;
    }

    public static bool operator !=(ClientVersion v1, ClientVersion v2)
    {
        return !(v1 == v2);
    }

    public override bool Equals(object obj)
    {
        if (obj is ClientVersion other)
        {
            return this == other;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _version != null ? _version.GetHashCode() : 0;
    }
}

public class VersionRange
{
    public string From { get; set; }
    public string To { get; set; }
}