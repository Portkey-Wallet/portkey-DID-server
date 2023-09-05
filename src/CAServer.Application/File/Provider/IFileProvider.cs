using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using CAServer.Options;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.File.Provider;

public interface IFileProvider
{
    public string FileDownLoad(string url);

    public Task<string> FileFormatConvertAndUpload(string path);
}

public class FileProvider : IFileProvider, ISingletonDependency
{
    private readonly Logger<FileProvider> _logger;
    public const string FileDownloadPath = "/opt/portkey-did-server/Download/Image";
    public const string FileOutputPath = "/opt/portkey-did-server/Output/Image";
    public const string DefaultsSuffix = "png";
    public const string DefaultBucketName = "YOU Bucket Name";
    public const string DefaultsImageName = "YOU Image Name";

    private readonly AmazonS3Client _s3Client;
    private readonly AWSS3Options _awsS3Options;

    public FileProvider(Logger<FileProvider> logger, IOptions<AWSS3Options> awsS3Options)
    {
        _logger = logger;
        _awsS3Options = awsS3Options.Value;
        _s3Client = new AmazonS3Client(_awsS3Options.AwsAccessKeyId, _awsS3Options.AwsSecretAccessKeyId,
            RegionEndpoint.GetBySystemName(_awsS3Options.SystemName));
    }

    public string FileDownLoad(string url)
    {
        var suffix = GetImageUrlSuffix(url);
        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var localFilePath = FileDownloadPath + timeStamp + "." + suffix;
        using var client = new WebClient();
        try
        {
            client.DownloadFile(url, localFilePath);
            _logger.LogDebug("Download file success.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return localFilePath;
    }

    public async Task<string> FileFormatConvertAndUpload(string inputPath)
    {
        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var outputImagePath = FileOutputPath + timeStamp + "." + DefaultsSuffix;
        try
        {
            using var image = new MagickImage(inputPath);
            image.Format = MagickFormat.Png;
            if (Directory.Exists(outputImagePath))
            {
                Directory.Delete(outputImagePath);
            }
            else
            {
                Directory.CreateDirectory(outputImagePath);
            }

            await image.WriteAsync(outputImagePath);
            _logger.LogDebug("image format convert success！");
        }
        catch (MagickException ex)
        {
            _logger.LogError($"image format failed : {ex.Message}");
        }

        var fileTransferUtility = new TransferUtility(_s3Client);
        try
        {
            await fileTransferUtility.UploadAsync(outputImagePath, DefaultBucketName, DefaultsImageName);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogDebug("upload file to S3 failed:{error}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("upload file failed:{error}", ex.Message);
            throw;
        }

        if (Directory.Exists(outputImagePath))
        {
            Directory.Delete(outputImagePath);
        }
        return outputImagePath;
    }


    private string GetImageUrlSuffix(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var imageUrlArray = imageUrl.Split(".");
        return imageUrlArray[^1].ToLower();
    }
}