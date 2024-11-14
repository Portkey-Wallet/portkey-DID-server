using System.Collections.Generic;
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

    public static readonly Dictionary<string, string> ChainNameMap = new Dictionary<string, string>
    {
        [CommonConstant.MainChainId] = "aelf MainChain",
        [CommonConstant.TDVWChainId] = "aelf dAppChain",
        [CommonConstant.TDVVChainId] = "aelf dAppChain",
        ["ARBITRUM"] = "Arbitrum One",
        ["AVAXC"] = "AVAX C-Chain",
        ["Base"] = "Base", // need to confirm
        ["BSC"] = "BNB Smart Chain",
        ["TBSC"] = "BNB Smart Chain",
        ["ETH"] = "Ethereum",
        ["SETH"] = "Ethereum",
        ["OPTIMISM"] = "Optimism",
        ["MATIC"] = "Polygon",
        ["Solana"] = "Solana",
        ["TRX"] = "TRON",
        ["TON"] = "The Open Network"
    };

    public static string GetNetworkName(string network)
    {
        return ChainNameMap.GetOrDefault(network);
    }

    public static string GetNetwork(string network)
    {
        return network is CommonConstant.MainChainId or CommonConstant.TDVWChainId or CommonConstant.TDVWChainId
            ? "aelf"
            : network;
    }

    public static string GetAelfChainId(string network)
    {
        return network is CommonConstant.MainChainId or CommonConstant.TDVWChainId or CommonConstant.TDVWChainId
            ? network
            : null;
    }

    public static bool CheckAddress(string network, string address)
    {
        if (network == "ETH")
        {
            return address.Length == 42;
        }
        else if (network == "aelf")
        {
            return AElf.AddressHelper.VerifyFormattedAddress(ToShortAddress(address));
        }
        else if (network == "BSC")
        {
            return address.Length == 42;
        }

        // if (address.Length == 50)
        // {
        //     if (fromChain == CommonConstant.MainChainId)
        //     {
        //         return AddressFormat.Main;
        //     }
        //
        //     return AddressFormat.Dapp;
        // }
        //
        // if (address.Length == 42)
        // {
        //     return AddressFormat.ETH;
        // }
        //
        // if (address.Length == 34)
        // {
        //     return AddressFormat.TRX;
        // }
        //
        // if (address.Length == 43 || address.Length == 44)
        // {
        //     return AddressFormat.Solana;
        // }
        //
        // if (address.Length == 48)
        // {
        //     return AddressFormat.TON;
        // }

        return false;
    }
}