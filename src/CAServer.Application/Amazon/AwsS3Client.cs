using System;
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
using Microsoft.Extensions.Logging;
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
    private readonly IOptionsMonitor<AwsS3Option> _awsS3Option;
    private readonly ISecretProvider _secretProvider;
    private readonly IOptionsMonitor<SignatureServerOptions> _signatureOptions;
    private AmazonS3Client _amazonS3Client;
    private readonly ILogger<AwsS3Client> _logger;

    public AwsS3Client(IOptionsMonitor<AwsS3Option> awsS3Option, ISecretProvider secretProvider,
        IOptionsMonitor<SignatureServerOptions> signatureOptions,ILogger<AwsS3Client> logger)
    {
        _awsS3Option = awsS3Option;
        _secretProvider = secretProvider;
        _signatureOptions = signatureOptions;
        _logger = logger;
        InitAmazonS3Client();
    }

    private void InitAmazonS3Client()
    {
        try
        {
            var identityPoolId = AsyncHelper.RunSync(() =>
                _secretProvider.GetSecretWithCacheAsync(_signatureOptions.CurrentValue.KeyIds.AwsS3IdentityPool));
            var cognitoCredentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.APNortheast1);
            _amazonS3Client = new AmazonS3Client(cognitoCredentials, RegionEndpoint.APNortheast1);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not initialize AWS S3 client");
            Console.WriteLine(e);
            throw;
        }
    }


    public async Task<string> UpLoadFileAsync(Stream steam, string fileName)
    {
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = steam,
            BucketName = _awsS3Option.CurrentValue.BucketName,
            Key = _awsS3Option.CurrentValue.S3Key + "/images/svg/" + fileName + ".svg",
            CannedACL = S3CannedACL.PublicRead,
        };
        var putObjectResponse = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        return putObjectResponse.HttpStatusCode == HttpStatusCode.OK
            ? $"https://{_awsS3Option.CurrentValue.BucketName}.s3.amazonaws.com/{putObjectRequest.Key}"
            : string.Empty;
    }
}