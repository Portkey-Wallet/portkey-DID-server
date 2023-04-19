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
    private readonly IHttpService _httpService;
    private readonly ILogger<ImageProcessProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private HttpClient? Client { get; set; }

    public ImageProcessProvider(ILogger<ImageProcessProvider> logger,
        IOptions<AdaptableVariableOptions> adaptableVariableOptions, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpService = new HttpService(adaptableVariableOptions.Value.HttpConnectTimeOut, _httpClientFactory, true);
    }

    public string GetResizeImage(string imageUrl, int width, int height)
    {
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

            var produceImage = getResizeUrl(imageUrl, width, height, true);
            sendUrl(produceImage);

            var resImage = getResizeUrl(imageUrl, width, height, false);
            return resImage;
        }
        catch (Exception ex)
        {
            _logger.LogError("sendImageRequest Execption:", ex);
            return imageUrl;
        }
    }

    public string getResizeUrl(string imageUrl, int width, int height, bool replaceDomain)
    {
        if (replaceDomain)
        {
            string[] urlSplit = imageUrl.Split(new string[] { UserAssetsServiceConstant.AwsDomain }, StringSplitOptions.RemoveEmptyEntries);
            imageUrl = UserAssetsServiceConstant.NewAwsDomain + urlSplit[1];
        }

        int lastIndexOf = imageUrl.LastIndexOf("/");
        var pre = imageUrl.Substring(0, lastIndexOf);
        var last = imageUrl.Substring(lastIndexOf, imageUrl.Length - lastIndexOf);
        var resizeImage = pre + "/" + (width == -1 ? "AUTO" : width) + "x" + (height == -1 ? "AUTO" : height) + last;
        return resizeImage;
    }

    private void sendUrl(string url, string? version = null)
    {
        if (Client == null)
        {
            Client = new HttpClient();
        }

        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(
            MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
        Client.DefaultRequestHeaders.Add("Connection", "close");
        Client.GetAsync(url);
    }
}