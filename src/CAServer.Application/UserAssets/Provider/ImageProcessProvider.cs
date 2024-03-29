using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CAActivity;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Entities.Es;
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
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.UserAssets.Provider;

public class ImageProcessProvider : IImageProcessProvider, ISingletonDependency
{
    private readonly ILogger<ImageProcessProvider> _logger;
    private readonly AwsThumbnailOptions _awsThumbnailOptions;

    private HttpClient? Client { get; set; }

    public ImageProcessProvider(ILogger<ImageProcessProvider> logger,
        IOptions<AwsThumbnailOptions> awsThumbnailOptions)
    {
        _logger = logger;
        _awsThumbnailOptions = awsThumbnailOptions.Value;
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