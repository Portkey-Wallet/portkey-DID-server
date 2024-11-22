using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CAServer.Transfer.Dtos;

namespace CAServer.Commons;

public static class ShiftChainHelper
{
    public static NetworkInfoDto GetNetworkInfoByEBridge(Dictionary<string, NetworkInfoDto> networkMap, string chain)
    {
        string formatChain = FormatEBridgeChain(chain);
        if (networkMap.ContainsKey("SETH") && formatChain == "ETH")
        {
            formatChain = "SETH";
        }
        if (networkMap.ContainsKey("TBSC") && formatChain == "BSC")
        {
            formatChain = "TBSC";
        }

        if (networkMap.ContainsKey("BSC") && formatChain == "TBSC")
        {
            formatChain = "BSC";
        }

        if (networkMap.TryGetValue(formatChain, out var existingNetworkInfo))
        {
            return new NetworkInfoDto
            {
                Name = existingNetworkInfo.Name,
                Network = existingNetworkInfo.Network,
                ImageUrl = existingNetworkInfo.ImageUrl
            };
        }

        return new NetworkInfoDto
        {
            Name = formatChain,
            Network = formatChain,
            ImageUrl = ChainDisplayNameHelper.DAppChainImageUrl
        };
    }

    public static string GetTime(string fromChain, string toChain)
    {
        string formatFromChain = FormatEBridgeChain(fromChain);
        string formatToChain = FormatEBridgeChain(toChain);
        if (formatFromChain == "ETH")
        {
            return "40 mins";
        }
        else if (formatFromChain == "BSC")
        {
            return "10 mins";
        }
        else if (formatFromChain == CommonConstant.TDVVChainId || formatFromChain == CommonConstant.TDVWChainId ||
                 formatFromChain == CommonConstant.MainChainId)
        {
            return "8 mins";
        }

        return "40 mins";
    }

    private static readonly Dictionary<string, string> ChainMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Ethereum", "ETH" },
        { "Sepolia", "ETH" },
        { "BSCTest", "BSC" },
        { "BaseSepolia", "Base" }
    };

    public static string FormatEBridgeChain(string chain)
    {
        if (ChainMappings.TryGetValue(chain, out string formattedChain))
        {
            return formattedChain;
        }

        return chain;
    }


    public const string ETransferTool = "ETransfer";
    public const string EBridgeTool = "EBridge";

    public static readonly Dictionary<string, ChainInfo> ChainInfoMap = new Dictionary<string, ChainInfo>
    {
        { CommonConstant.MainChainId, new ChainInfo(CommonConstant.MainChainId, ChainDisplayNameHelper.MainChainImageUrl, AddressFormat.Main) },
        { CommonConstant.TDVWChainId, new ChainInfo(CommonConstant.TDVWChainId, ChainDisplayNameHelper.DAppChainImageUrl, AddressFormat.Dapp) },
        { CommonConstant.TDVVChainId, new ChainInfo(CommonConstant.TDVVChainId, ChainDisplayNameHelper.DAppChainImageUrl, AddressFormat.Dapp) },
        { "ARBITRUM", new ChainInfo("ARBITRUM", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainArbitrum.png", AddressFormat.ETH) },
        { "AVAXC", new ChainInfo("AVAXC", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainAvalanche.png", AddressFormat.ETH) },
        { "Base", new ChainInfo("Base", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainBase.png", AddressFormat.ETH) },
        { "BSC", new ChainInfo("BSC", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainBinance.png", AddressFormat.ETH) },
        { "TBSC", new ChainInfo("BSC", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainBinance.png", AddressFormat.ETH) },
        { "ETH", new ChainInfo("ETH", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainEthereum.png", AddressFormat.ETH) },
        { "SETH", new ChainInfo("SETH", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainEthereum.png", AddressFormat.ETH) },
        { "OPTIMISM", new ChainInfo("OPTIMISM", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainOptimism.png", AddressFormat.ETH) },
        { "MATIC", new ChainInfo("MATIC", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainPolygon.png", AddressFormat.ETH) },
        { "Solana", new ChainInfo("Solana", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainSolana.png", AddressFormat.Solana) },
        { "TRX", new ChainInfo("TRX", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainTron.png", AddressFormat.TRX) },
        { "TON", new ChainInfo("TON", "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/chain/ChainTON.png", AddressFormat.TON) },
    };
    
    public static readonly Dictionary<string, string> NetworkPatternMap = new Dictionary<string, string>
    {
        { "SETH", "^0x[a-fA-F0-9]{40}$" },
        { "ETH", "^0x[a-fA-F0-9]{40}$" },
        { "BSC", "^0x[a-fA-F0-9]{40}$" },
        { "ARBITRUM", "^0x[a-fA-F0-9]{40}$" },
        { "MATIC", "^0x[a-fA-F0-9]{40}$" },
        { "OPTIMISM", "^0x[a-fA-F0-9]{40}$" },
        { "AVAXC", "^0x[a-fA-F0-9]{40}$" },
        { "Base", "^0x[a-fA-F0-9]{40}$" },
        { "TRX", "^T[1-9A-HJ-NP-Za-km-z]{33}$" },
        { "Solana", "^[1-9A-HJ-NP-Za-km-z]{32,44}$" },
        { "TON", "^[EU]Q[a-zA-Z0-9_-]{46}$" },
    };

    public static string GetChainImage(string network)
    {
        return ChainInfoMap.TryGetValue(network, out ChainInfo image)
            ? image.ImageUrl
            : ChainDisplayNameHelper.DAppChainImageUrl;
    }

    public const decimal ETransferMaxAmountUsd = 20000;

    public static string GetMaxAmount(decimal priceInUsd)
    {
        if (priceInUsd.CompareTo(0) == 0)
        {
            return ETransferMaxAmountUsd.ToString();
        }

        return Math.Floor(ETransferMaxAmountUsd / priceInUsd).ToString();
    }

    public static NetworkInfoDto GetAELFInfo(string chainId)
    {
        return new NetworkInfoDto
        {
            Network = chainId,
            Name = ChainDisplayNameHelper.MustGetChainDisplayName(chainId),
            ImageUrl = ChainDisplayNameHelper.MustGetChainUrl(chainId)
        };
    }

    public static bool MatchForAddress(string chain, string fromChain, string address)
    {
        if (!ChainInfoMap.TryGetValue(chain, out var info))
        {
            return false;
        }

        AddressFormat format = GetAddressFormat(fromChain, address);
        return info.AddressFormat == format;
    }

    public static AddressFormat GetAddressFormat(string fromChain, string address)
    {
        if (address.Split("_").Length == 3 && address.Split("_")[1].Length == 50)
        {
            if (address.EndsWith(CommonConstant.MainChainId))
            {
                return AddressFormat.Main;
            }else if (address.EndsWith(CommonConstant.TDVWChainId) || address.EndsWith(CommonConstant.TDVVChainId))
            {
                return AddressFormat.Dapp;
            }

            return AddressFormat.NoSupport;
        }

        if (IsAelfAddress(address))
        {
            if (fromChain == CommonConstant.MainChainId)
            {
                return AddressFormat.Main;
            }

            return AddressFormat.Dapp;
        }

        if (address.Length == 42)
        {
            return AddressFormat.ETH;
        }

        if (address.Length == 34)
        {
            return AddressFormat.TRX;
        }

        if (address.Length == 43 || address.Length == 44)
        {
            return AddressFormat.Solana;
        }

        if (address.Length == 48 && (address.StartsWith("EQ") || address.StartsWith("UQ")))
        {
            return AddressFormat.TON;
        }

        return AddressFormat.NoSupport;
    }

    public static string ExtractAddress(string addressSuffix)
    {
        return addressSuffix
            .Split('_')
            .FirstOrDefault(p => p.Length == 50) ?? addressSuffix;
    }
    
    private static bool IsAelfAddress(string address)
    {
        try
        {
            return AElf.AddressHelper.VerifyFormattedAddress(address);
        }
        catch
        {
            return false;
        }
    }
    
    public static bool VerifyAddress(string chain, string address)
    {
        if (!ChainInfoMap.TryGetValue(chain, out var info))
        {
            return false;
        }
        
        if(chain is CommonConstant.MainChainId or CommonConstant.TDVVChainId or CommonConstant.TDVWChainId)
        {
            return AElf.AddressHelper.VerifyFormattedAddress(address);
        } 
        
        return !NetworkPatternMap.ContainsKey(chain) || Regex.IsMatch(address, NetworkPatternMap[chain]);
    }
}

public class ChainInfo
{
    public string Network { get; set; }
    public string ImageUrl { get; set; }
    public AddressFormat AddressFormat { get; set; }

    public ChainInfo(string network, string imageUrl, AddressFormat addressFormat)
    {
        Network = network;
        ImageUrl = imageUrl;
        AddressFormat = addressFormat;
    }
}

public enum AddressFormat
{
    Main,
    Dapp,
    ETH,
    TRX,
    TON,
    Solana,
    NoSupport,
}