using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using CAServer.Amazon;
using CAServer.Signature.Options;
using CAServer.Signature.Provider;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace CAServer.amazon;

public interface IAwsS3Client
{
    Task<string> UpLoadFileAsync(Stream steam, string fileName);
}

public class AwsS3Client : IAwsS3Client, ISingletonDependency
{
    private readonly AwsS3Option _awsS3Option;
    private readonly ISecretProvider _secretProvider;
    private readonly IOptionsMonitor<SignatureServerOptions> _signatureOptions;
    private AmazonS3Client _amazonS3Client;

    public AwsS3Client(AwsS3Option awsS3Option, ISecretProvider secretProvider,
        IOptionsMonitor<SignatureServerOptions> signatureOptions)
    {
        _awsS3Option = awsS3Option;
        _secretProvider = secretProvider;
        _signatureOptions = signatureOptions;
        InitAmazonS3Client();
    }

    private void InitAmazonS3Client()
    {
        var identityPoolId = AsyncHelper.RunSync(() =>
            _secretProvider.GetSecretWithCacheAsync(_signatureOptions.CurrentValue.KeyIds.AwsS3IdentityPool));
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
        var putObjectResponse = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        return putObjectResponse.HttpStatusCode == HttpStatusCode.OK
            ? $"https://{_awsS3Option.BucketName}.s3.amazonaws.com/{putObjectRequest.Key}"
            : string.Empty;
    }
}