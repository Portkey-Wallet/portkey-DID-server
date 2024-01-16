using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using CAServer.Amazon;
using CAServer.Signature.Provider;
using Volo.Abp.DependencyInjection;

namespace CAServer.amazon;

public class AwsS3Client : ISingletonDependency
{
    private readonly AwsS3Option _awsS3Option;
    private readonly ISecretProvider _secretProvider;
    private  AmazonS3Client _amazonS3Client;

    public AwsS3Client(AwsS3Option awsS3Option, ISecretProvider secretProvider)
    {
        _awsS3Option = awsS3Option;
        _secretProvider = secretProvider;
        InitAmazonS3Client();
    }

    private void InitAmazonS3Client()
    {
        var identityPoolId = _awsS3Option.IdentityPoolId;
        var cognitoCredentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.APNortheast1);
        _amazonS3Client = new AmazonS3Client(cognitoCredentials, RegionEndpoint.APNortheast1);
    }
    

    public async Task<string> UpLoadFileAsync(Stream steam, string fileName)
    {
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = steam,
            BucketName = _awsS3Option.BucketName,
            Key = _awsS3Option.S3Key + "/images/svg/" + fileName + ".svg",
            CannedACL = S3CannedACL.PublicRead,
        };
        var putObjectResponse  = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        return putObjectResponse.HttpStatusCode == HttpStatusCode.OK ? 
            $"https://{_awsS3Option.BucketName}.s3.amazonaws.com/{putObjectRequest.Key}" 
            : string.Empty;
    }
}
