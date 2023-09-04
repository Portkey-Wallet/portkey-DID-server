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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AwsThumbnailOptions _awsThumbnailOptions;
    private HttpClient? Client { get; set; }

    public ImageProcessProvider(ILogger<ImageProcessProvider> logger,
        IHttpClientFactory httpClientFactory, IOptions<AwsThumbnailOptions> awsThumbnailOptions)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _awsThumbnailOptions = awsThumbnailOptions.Value;
    }

    public async Task<string> GetResizeImageAsync(string imageUrl, int width, int height)
    {
        _logger.LogDebug("Received GetResizeImageAsync request.ImgUrl:{imageUrl},width:{width},height:{height}",
            imageUrl, width,
            height);
        try
        {
            if (!imageUrl.Contains(UserAssetsServiceConstant.AwsDomain))
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

            var produceImage = GetResizeUrl(imageUrl, width, height, true, ImageResizeType.PortKey);
            await SendUrlAsync(produceImage);
            _logger.LogDebug("Compress image success.produceImage:{produceImage},width:{width},height:{height}",
                produceImage, width, height);

            var resImage = GetResizeUrl(imageUrl, width, height, false, ImageResizeType.PortKey);
            _logger.LogDebug("View image success.resImage:{resImage},width:{width},height:{height}", resImage, width,
                height);
            return resImage;
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

            var produceImage = GetResizeUrl(imageUrl, width, height, true, ImageResizeType.Im);
            await SendUrlAsync(produceImage);

            var resImage = GetResizeUrl(imageUrl, width, height, false, ImageResizeType.Im);
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
}