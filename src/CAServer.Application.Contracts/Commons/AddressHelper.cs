using System.Linq;

namespace CAServer.Commons;

public static class AddressHelper
{
    private const string FullAddressPrefix = "ELF";
    private const char FullAddressSeparator = '_';

    public static string ToFullAddress(string address, string chainId)
    {
        if (address.Contains(FullAddressSeparator)) return address;
        
        return string.Join(FullAddressSeparator, FullAddressPrefix, address, chainId);
    }

    public static string ToShortAddress(string address)
    {
        if (string.IsNullOrEmpty(address)) return address;
        var parts = address.Split(FullAddressSeparator);
        return parts.Length < 3 ? parts[parts.Length - 1] : parts[1];
    }
    
    public static string GetChainId(string address)
    {
        if (string.IsNullOrEmpty(address)) return string.Empty;
        var chainId = address.Split(FullAddressSeparator).ToList().Last();
        return chainId.Length != 4 ? string.Empty : chainId;
    }
}