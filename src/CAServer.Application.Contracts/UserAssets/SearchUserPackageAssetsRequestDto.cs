namespace CAServer.UserAssets;

public class SearchUserPackageAssetsRequestDto : SearchUserAssetsRequestDto
{
    public int AssetType { get; set; }
    public string Version { get; set; }
}