using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CAServer.amazon;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.Svg;
using CAServer.Image.Dto;
using CAServer.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace CAServer.UserAssets.Provider;

public class ImageProcessProvider : IImageProcessProvider, ISingletonDependency
{
    private readonly ILogger<ImageProcessProvider> _logger;
    private readonly AwsThumbnailOptions _awsThumbnailOptions;
    private readonly IClusterClient _clusterClient;
    private readonly IAwsS3Client _awsS3Client;


    private HttpClient Client { get; set; }

    public ImageProcessProvider(ILogger<ImageProcessProvider> logger,
        IOptions<AwsThumbnailOptions> awsThumbnailOptions,
        IClusterClient clusterClient, IAwsS3Client awsS3Client)
    {
        _logger = logger;
        _awsThumbnailOptions = awsThumbnailOptions.Value;
        _clusterClient = clusterClient;
        _awsS3Client = awsS3Client;
    }

    public async Task<string> GetResizeImageAsync(string imageUrl, int width, int height, ImageResizeType type)
    {
        try
        {
            if (!imageUrl.StartsWith(CommonConstant.ProtocolName))
            {
                return imageUrl;
            }

            if (!_awsThumbnailOptions.ExcludedSuffixes.Contains(GetImageUrlSuffix(imageUrl)))
            {
                return imageUrl;
            }

            var bucket = imageUrl.Split("/")[2];
            if (!_awsThumbnailOptions.BucketList.Contains(bucket))
            {
                return imageUrl;
            }

            var resizeWidth = Enum.GetValues(typeof(ImageResizeWidthType)).Cast<ImageResizeWidthType>()
                .FirstOrDefault(a => (int)a == width);

            var reizeHeight = Enum.GetValues(typeof(ImageResizeHeightType)).Cast<ImageResizeHeightType>()
                .FirstOrDefault(a => (int)a == height);

            if (resizeWidth == ImageResizeWidthType.None || reizeHeight == ImageResizeHeightType.None)
            {
                return imageUrl;
            }

            return await GetResizeImageUrlAsync(imageUrl, width, height);
        }
        catch (Exception ex)
        {
            _logger.LogError("sendImageRequest Execption:", ex);
            return imageUrl;
        }
    }

    public async Task<ThumbnailResponseDto> GetImResizeImageAsync(string imageUrl, int width, int height)
    {
        try
        {
            if (!imageUrl.Contains(UserAssetsServiceConstant.AwsDomain))
            {
                return new ThumbnailResponseDto();
            }

            var resImage = await GetResizeImageUrlAsync(imageUrl, width, height);
            return new ThumbnailResponseDto
            {
                ThumbnailUrl = resImage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("sendImageRequest Execption:", ex);
            return new ThumbnailResponseDto();
        }
    }

    public async Task<string> UploadSvgAsync(string svgMd5, string svg = null)
    {
        var grain = _clusterClient.GetGrain<ISvgGrain>(svgMd5);
        var svgGrainDto = await grain.GetSvgAsync();
        svg ??= svgGrainDto.Svg;
        var amazonUrl = svgGrainDto.AmazonUrl;
        svgGrainDto.Id = svgMd5;
        svgGrainDto.AmazonUrl = "";
        svgGrainDto.Svg = svg;
        AssertHelper.NotEmpty(svg, "svg is not exist");

        svgGrainDto.Svg = svg;
        //if exist return result
        if (amazonUrl.NotNullOrEmpty())
        {
            return amazonUrl;
        }

        //upload the svg to amazon and get its url
        var byteData = Encoding.UTF8.GetBytes(svg);
        try
        {
            var res = await _awsS3Client.UpLoadFileAsync(new MemoryStream(byteData), svgMd5);
            svgGrainDto.AmazonUrl = res;
            await grain.AddSvgAsync(svgGrainDto);
            _logger.LogDebug("Aws S3 upload to {Rul}", svgGrainDto.AmazonUrl);
            return res;
        }
        catch (Exception e)
        {
            _logger.LogError("upload to amazon svg fail,exception is", e);
            return "upload to amazon svg fail";
        }
    }


    public string GetResizeUrl(string imageUrl, int width, int height, bool replaceDomain, ImageResizeType type)
    {
        if (replaceDomain)
        {
            var urlSplit = imageUrl.Split(new string[] { UserAssetsServiceConstant.AwsDomain },
                StringSplitOptions.RemoveEmptyEntries);
            imageUrl = type switch
            {
                ImageResizeType.PortKey => _awsThumbnailOptions.PortKeyBaseUrl + urlSplit[1],
                ImageResizeType.Im => _awsThumbnailOptions.ImBaseUrl + urlSplit[1],
                ImageResizeType.Forest => _awsThumbnailOptions.ForestBaseUrl + urlSplit[1],
                _ => imageUrl
            };
        }

        var lastIndexOf = imageUrl.LastIndexOf("/", StringComparison.Ordinal);
        var pre = imageUrl.Substring(0, lastIndexOf);
        var last = imageUrl.Substring(lastIndexOf, imageUrl.Length - lastIndexOf);
        var resizeImage = pre + "/" + (width == -1 ? "AUTO" : width) + "x" + (height == -1 ? "AUTO" : height) + last;
        return resizeImage;
    }

    private async Task SendUrlAsync(string url, string? version = null)
    {
        Client ??= new HttpClient();
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(
            MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
        Client.DefaultRequestHeaders.Add("Connection", "close");
        await Client.GetAsync(url);
    }

    private async Task<string> GetResizeImageUrlAsync(string imageUrl, int width, int height)
    {
        var type = GetS3Type(imageUrl);
        var produceImage = GetResizeUrl(imageUrl, width, height, true, type);
        await SendUrlAsync(produceImage);

        var resImage = GetResizeUrl(imageUrl, width, height, false, type);
        return resImage;
    }

    private ImageResizeType GetS3Type(string imageUrl)
    {
        var urlSplit = imageUrl.Split(new string[] { UserAssetsServiceConstant.AwsDomain },
            StringSplitOptions.RemoveEmptyEntries);

        if (urlSplit[0].ToLower().Contains(CommonConstant.ImS3Mark))
        {
            return ImageResizeType.Im;
        }

        if (urlSplit[0].ToLower().Contains(CommonConstant.PortkeyS3Mark))
        {
            return ImageResizeType.PortKey;
        }

        return ImageResizeType.Forest;
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