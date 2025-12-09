namespace CAServer.Amazon;

public class AwsS3Option
{
    public string IdentityPoolId { get; set; }
    public string BucketName { get; set; }
    public string S3Key { get; set; }

    public string RegionEndpoint { get; set; } = "ap-northeast-1";

}