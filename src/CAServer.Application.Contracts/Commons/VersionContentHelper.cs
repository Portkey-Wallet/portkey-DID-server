using System;
using System.Linq;
using CAServer.UserAssets.Dtos;

namespace CAServer.Commons;

public static class VersionContentHelper
{
    public static SearchUserPackageAssetsDto FilterUserPackageAssetsByVersion(string version,
        SearchUserPackageAssetsDto userPackageAssets)
    {
        var inputVersion = GetInputVersion(version);
        return inputVersion == null || inputVersion >= new Version("1.8.1")
            ? userPackageAssets
            : FilterUserPackageAssets(userPackageAssets);
    }

    public static bool CompareVersion(string version, string compareVersion)
    {
        var inputVersion = GetInputVersion(version);
        return inputVersion != null && inputVersion >= new Version(compareVersion);
    }

    private static Version GetInputVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return null;
        }

        try
        {
            return new Version(version.ToLower().Trim().TrimStart('v'));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static SearchUserPackageAssetsDto FilterUserPackageAssets(SearchUserPackageAssetsDto userPackageAssets)
    {
        var filteredData = userPackageAssets.Data
            .Where(asset => asset.AssetType != (int)AssetType.NFT
                            || (!string.IsNullOrEmpty(asset.ImageUrl)
                                && asset.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        int ftRecordCount = 0;
        int nftRecordCount = 0;

        foreach (var asset in filteredData)
        {
            if (asset.AssetType == (int)AssetType.FT)
            {
                ftRecordCount++;
            }
            else
            {
                nftRecordCount++;
            }
        }

        return new SearchUserPackageAssetsDto
        {
            Data = filteredData,
            TotalRecordCount = filteredData.Count,
            FtRecordCount = ftRecordCount,
            NftRecordCount = nftRecordCount
        };
    }
}