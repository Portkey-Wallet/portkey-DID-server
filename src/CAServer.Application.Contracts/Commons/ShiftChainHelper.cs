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
    };

    public static string GetChainImage(string network)
    {
        return ChainInfoMap.TryGetValue(network, out ChainInfo image)
            ? image.ImageUrl
            : ChainDisplayNameHelper.DAppChainImageUrl;
    }

    public const decimal ETransferMaxAmountUsd = 50000;

    public static string GetMaxAmount(decimal priceInUsd)
    {
        if (priceInUsd.CompareTo(0) == 0)
        {
            return ETransferMaxAmountUsd.ToString();
        }

        return Math.Floor(priceInUsd / ETransferMaxAmountUsd).ToString();
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
        if (address.Contains("_"))
        {
            if (address.EndsWith(CommonConstant.MainChainId))
            {
                return AddressFormat.Main;
            }

            return AddressFormat.Dapp;
        }

        if (address.Length == 50)
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

        return AddressFormat.NoSupport;
    }
    
    public static string ExtractAddress(string addressSuffix)
    {
        return addressSuffix.Split("_").First(p => p.Length > 10);
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
    Solana,
    NoSupport,
}