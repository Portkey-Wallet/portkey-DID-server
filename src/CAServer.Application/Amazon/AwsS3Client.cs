using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using CAServer.Amazon;

namespace CAServer.amazon;




using System.Net;
using Volo.Abp.DependencyInjection;

public class AwsS3Client : ISingletonDependency
{
    private readonly AwsS3Option _awsS3Option;

    private  AmazonS3Client _amazonS3Client;

    public AwsS3Client(AwsS3Option awsS3Option)
    {
        _awsS3Option = awsS3Option;
        InitAmazonS3Client();
    }

    private void InitAmazonS3Client()
    {
        var identityPoolId = _awsS3Option.IdentityPoolId;
        var ServiceURL = _awsS3Option.ServiceURL;
        var cognitoCredentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.APNortheast1);
        _amazonS3Client = new AmazonS3Client(cognitoCredentials, RegionEndpoint.APNortheast1);
    }
    

    public async Task<string> UpLoadFileAsync(Stream steam, string fileName)
    {
        var putObjectRequest = new PutObjectRequest
        {
            InputStream = steam,
            BucketName = _awsS3Option.BucketName,
            Key = _awsS3Option.S3Key + "/" + fileName + ".svg",
            CannedACL = S3CannedACL.PublicRead,
        };
        var putObjectResponse  = await _amazonS3Client.PutObjectAsync(putObjectRequest);
        return putObjectResponse.HttpStatusCode == HttpStatusCode.OK ? 
            $"https://{_awsS3Option.BucketName}.s3.amazonaws.com/{_awsS3Option.S3Key}/{fileName}.svg" 
            : string.Empty;
    }

    public async Task<string> GetSpecialSymbolUrl(string fileName)
    {
        return $"https://{_awsS3Option.BucketName}.s3.amazonaws.com/{_awsS3Option.S3Key}/{fileName}.svg";
    }


    public async Task<GetObjectResponse> GetObjectAsync(string fileName)
    {
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = _awsS3Option.BucketName,
            Key = _awsS3Option.S3Key + "/" + fileName + ".svg"
        };
        var getObjectResponse = await _amazonS3Client.GetObjectAsync(getObjectRequest);
        return getObjectResponse;
    }
}
