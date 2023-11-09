using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.amazon;
using CAServer.CAActivity;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Google;
using CAServer.Grains.Grain.Svg;
using CAServer.Grains.Grain.Svg.Dtos;
using CAServer.Image.Dto;
using CAServer.Options;
using CAServer.Settings;
using CAServer.Tokens;
using CAServer.UserAssets.Dtos;
using CAServer.Verifier;
using Elasticsearch.Net;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.UserAssets.Provider;

public class ImageProcessProvider : IImageProcessProvider, ISingletonDependency
{
    private readonly ILogger<ImageProcessProvider> _logger;
    private readonly AwsThumbnailOptions _awsThumbnailOptions;
    private readonly IOptionsMonitor<AwsS3Option> _awsS3Option;
    private readonly IClusterClient _clusterClient;



    private HttpClient? Client { get; set; }

    public ImageProcessProvider(ILogger<ImageProcessProvider> logger,
        IOptions<AwsThumbnailOptions> awsThumbnailOptions, 
        IOptionsMonitor<AwsS3Option> awsS3Option,
        IClusterClient clusterClient)
    {
        _logger = logger;
        _awsS3Option = awsS3Option;
        _awsThumbnailOptions = awsThumbnailOptions.Value;
        _clusterClient = clusterClient;

    }

    public async Task<string> GetResizeImageAsync(string imageUrl, int width, int height, ImageResizeType type)
    {
        try
        {
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

            return await GetResizeImageUrlAsync(imageUrl, width, height, type);
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

            var resImage = await GetResizeImageUrlAsync(imageUrl, width, height, ImageResizeType.Im);
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

    public async Task<string> UploadSvgAsync(string svgMd5)
    {
        if (string.IsNullOrWhiteSpace(svgMd5))
        {
            throw new Exception("upload image can not be empty");
        }
        var grain = _clusterClient.GetGrain<ISvgGrain>(svgMd5);
        var svgGrainDto = grain.GetSvgAsync();
        var svg = svgGrainDto.Result.Svg;
        var amazonUrl = svgGrainDto.Result.AmazonUrl;
        if (string.IsNullOrWhiteSpace(svg))
        {
            throw new Exception("svg is not exist");
        }
        //if exist return result
        if (!string.IsNullOrWhiteSpace(amazonUrl))
        {
            return amazonUrl;
        }
        //select from grain judge if exist
        //var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"21px\" height=\"15px\" viewBox=\"0 0 21 15\" version=\"1.1\">\\n    <!-- Generator: sketchtool 46 (44423) - http://www.bohemiancoding.com/sketch -->\\n    <title>US</title>\\n    <desc>Created with sketchtool.</desc>\\n    <defs>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-1\">\\n            <stop stop-color=\"#FFFFFF\" offset=\"0%\"/>\\n            <stop stop-color=\"#F0F0F0\" offset=\"100%\"/>\\n        </linearGradient>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-2\">\\n            <stop stop-color=\"#D02F44\" offset=\"0%\"/>\\n            <stop stop-color=\"#B12537\" offset=\"100%\"/>\\n        </linearGradient>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-3\">\\n            <stop stop-color=\"#46467F\" offset=\"0%\"/>\\n            <stop stop-color=\"#3C3C6D\" offset=\"100%\"/>\\n        </linearGradient>\\n    </defs>\\n    <g id=\"Symbols\" stroke=\"none\" stroke-width=\"1\" fill=\"none\" fill-rule=\"evenodd\">\\n        <g id=\"US\">\\n            <rect id=\"FlagBackground\" fill=\"url(#linearGradient-1)\" x=\"0\" y=\"0\" width=\"21\" height=\"15\"/>\\n            <path d=\"M0,0 L21,0 L21,1 L0,1 L0,0 Z M0,2 L21,2 L21,3 L0,3 L0,2 Z M0,4 L21,4 L21,5 L0,5 L0,4 Z M0,6 L21,6 L21,7 L0,7 L0,6 Z M0,8 L21,8 L21,9 L0,9 L0,8 Z M0,10 L21,10 L21,11 L0,11 L0,10 Z M0,12 L21,12 L21,13 L0,13 L0,12 Z M0,14 L21,14 L21,15 L0,15 L0,14 Z\" id=\"Rectangle-511\" fill=\"url(#linearGradient-2)\"/>\\n            <rect id=\"Rectangle-511\" fill=\"url(#linearGradient-3)\" x=\"0\" y=\"0\" width=\"9\" height=\"7\"/>\\n            <path d=\"M1.5,2 C1.22385763,2 1,1.77614237 1,1.5 C1,1.22385763 1.22385763,1 1.5,1 C1.77614237,1 2,1.22385763 2,1.5 C2,1.77614237 1.77614237,2 1.5,2 Z M3.5,2 C3.22385763,2 3,1.77614237 3,1.5 C3,1.22385763 3.22385763,1 3.5,1 C3.77614237,1 4,1.22385763 4,1.5 C4,1.77614237 3.77614237,2 3.5,2 Z M5.5,2 C5.22385763,2 5,1.77614237 5,1.5 C5,1.22385763 5.22385763,1 5.5,1 C5.77614237,1 6,1.22385763 6,1.5 C6,1.77614237 5.77614237,2 5.5,2 Z M7.5,2 C7.22385763,2 7,1.77614237 7,1.5 C7,1.22385763 7.22385763,1 7.5,1 C7.77614237,1 8,1.22385763 8,1.5 C8,1.77614237 7.77614237,2 7.5,2 Z M2.5,3 C2.22385763,3 2,2.77614237 2,2.5 C2,2.22385763 2.22385763,2 2.5,2 C2.77614237,2 3,2.22385763 3,2.5 C3,2.77614237 2.77614237,3 2.5,3 Z M4.5,3 C4.22385763,3 4,2.77614237 4,2.5 C4,2.22385763 4.22385763,2 4.5,2 C4.77614237,2 5,2.22385763 5,2.5 C5,2.77614237 4.77614237,3 4.5,3 Z M6.5,3 C6.22385763,3 6,2.77614237 6,2.5 C6,2.22385763 6.22385763,2 6.5,2 C6.77614237,2 7,2.22385763 7,2.5 C7,2.77614237 6.77614237,3 6.5,3 Z M7.5,4 C7.22385763,4 7,3.77614237 7,3.5 C7,3.22385763 7.22385763,3 7.5,3 C7.77614237,3 8,3.22385763 8,3.5 C8,3.77614237 7.77614237,4 7.5,4 Z M5.5,4 C5.22385763,4 5,3.77614237 5,3.5 C5,3.22385763 5.22385763,3 5.5,3 C5.77614237,3 6,3.22385763 6,3.5 C6,3.77614237 5.77614237,4 5.5,4 Z M3.5,4 C3.22385763,4 3,3.77614237 3,3.5 C3,3.22385763 3.22385763,3 3.5,3 C3.77614237,3 4,3.22385763 4,3.5 C4,3.77614237 3.77614237,4 3.5,4 Z M1.5,4 C1.22385763,4 1,3.77614237 1,3.5 C1,3.22385763 1.22385763,3 1.5,3 C1.77614237,3 2,3.22385763 2,3.5 C2,3.77614237 1.77614237,4 1.5,4 Z M2.5,5 C2.22385763,5 2,4.77614237 2,4.5 C2,4.22385763 2.22385763,4 2.5,4 C2.77614237,4 3,4.22385763 3,4.5 C3,4.77614237 2.77614237,5 2.5,5 Z M4.5,5 C4.22385763,5 4,4.77614237 4,4.5 C4,4.22385763 4.22385763,4 4.5,4 C4.77614237,4 5,4.22385763 5,4.5 C5,4.77614237 4.77614237,5 4.5,5 Z M6.5,5 C6.22385763,5 6,4.77614237 6,4.5 C6,4.22385763 6.22385763,4 6.5,4 C6.77614237,4 7,4.22385763 7,4.5 C7,4.77614237 6.77614237,5 6.5,5 Z M7.5,6 C7.22385763,6 7,5.77614237 7,5.5 C7,5.22385763 7.22385763,5 7.5,5 C7.77614237,5 8,5.22385763 8,5.5 C8,5.77614237 7.77614237,6 7.5,6 Z M5.5,6 C5.22385763,6 5,5.77614237 5,5.5 C5,5.22385763 5.22385763,5 5.5,5 C5.77614237,5 6,5.22385763 6,5.5 C6,5.77614237 5.77614237,6 5.5,6 Z M3.5,6 C3.22385763,6 3,5.77614237 3,5.5 C3,5.22385763 3.22385763,5 3.5,5 C3.77614237,5 4,5.22385763 4,5.5 C4,5.77614237 3.77614237,6 3.5,6 Z M1.5,6 C1.22385763,6 1,5.77614237 1,5.5 C1,5.22385763 1.22385763,5 1.5,5 C1.77614237,5 2,5.22385763 2,5.5 C2,5.77614237 1.77614237,6 1.5,6 Z\" id=\"Oval-43\" fill=\"url(#linearGradient-1)\"/>\\n        </g>\\n    </g>\\n</svg>";
        //upload the svg to amazon and get its url
        var client = new AwsS3Client(_awsS3Option.CurrentValue);
        var byteData = Encoding.UTF8.GetBytes(svg);
        try
        {
            var res= await client.UpLoadFileAsync(new MemoryStream(byteData), svg);
            svgGrainDto.Result.AmazonUrl = res;
            return res;
        }
        catch (Exception e)
        {
            _logger.LogError("upload to amazon svg fail,exception is",e);
            return "";
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

    private async Task<string> GetResizeImageUrlAsync(string imageUrl, int width, int height, ImageResizeType type)
    {
        var produceImage = GetResizeUrl(imageUrl, width, height, true, type);
        await SendUrlAsync(produceImage);

        var resImage = GetResizeUrl(imageUrl, width, height, false, type);
        return resImage;
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