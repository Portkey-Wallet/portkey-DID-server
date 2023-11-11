using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using CAServer.amazon;
using CAServer.Amazon;
using CAServer.Common;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.Svg;

public class SvgCommonTest :CAServerApplicationTestBase
{
    private ITestOutputHelper _testOutputHelper;
    private readonly IOptionsMonitor<AwsS3Option> _awsS3Option;


    public SvgCommonTest(ITestOutputHelper testOutputHelper, IOptionsMonitor<AwsS3Option> awsS3Option)
    {
        _testOutputHelper = testOutputHelper;
        _awsS3Option = awsS3Option;
    }

    [Fact]
    public async void UpLoadImageTest()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"21px\" height=\"15px\" viewBox=\"0 0 21 15\" version=\"1.1\">\\n    <!-- Generator: sketchtool 46 (44423) - http://www.bohemiancoding.com/sketch -->\\n    <title>US</title>\\n    <desc>Created with sketchtool.</desc>\\n    <defs>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-1\">\\n            <stop stop-color=\"#FFFFFF\" offset=\"0%\"/>\\n            <stop stop-color=\"#F0F0F0\" offset=\"100%\"/>\\n        </linearGradient>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-2\">\\n            <stop stop-color=\"#D02F44\" offset=\"0%\"/>\\n            <stop stop-color=\"#B12537\" offset=\"100%\"/>\\n        </linearGradient>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-3\">\\n            <stop stop-color=\"#46467F\" offset=\"0%\"/>\\n            <stop stop-color=\"#3C3C6D\" offset=\"100%\"/>\\n        </linearGradient>\\n    </defs>\\n    <g id=\"Symbols\" stroke=\"none\" stroke-width=\"1\" fill=\"none\" fill-rule=\"evenodd\">\\n        <g id=\"US\">\\n            <rect id=\"FlagBackground\" fill=\"url(#linearGradient-1)\" x=\"0\" y=\"0\" width=\"21\" height=\"15\"/>\\n            <path d=\"M0,0 L21,0 L21,1 L0,1 L0,0 Z M0,2 L21,2 L21,3 L0,3 L0,2 Z M0,4 L21,4 L21,5 L0,5 L0,4 Z M0,6 L21,6 L21,7 L0,7 L0,6 Z M0,8 L21,8 L21,9 L0,9 L0,8 Z M0,10 L21,10 L21,11 L0,11 L0,10 Z M0,12 L21,12 L21,13 L0,13 L0,12 Z M0,14 L21,14 L21,15 L0,15 L0,14 Z\" id=\"Rectangle-511\" fill=\"url(#linearGradient-2)\"/>\\n            <rect id=\"Rectangle-511\" fill=\"url(#linearGradient-3)\" x=\"0\" y=\"0\" width=\"9\" height=\"7\"/>\\n            <path d=\"M1.5,2 C1.22385763,2 1,1.77614237 1,1.5 C1,1.22385763 1.22385763,1 1.5,1 C1.77614237,1 2,1.22385763 2,1.5 C2,1.77614237 1.77614237,2 1.5,2 Z M3.5,2 C3.22385763,2 3,1.77614237 3,1.5 C3,1.22385763 3.22385763,1 3.5,1 C3.77614237,1 4,1.22385763 4,1.5 C4,1.77614237 3.77614237,2 3.5,2 Z M5.5,2 C5.22385763,2 5,1.77614237 5,1.5 C5,1.22385763 5.22385763,1 5.5,1 C5.77614237,1 6,1.22385763 6,1.5 C6,1.77614237 5.77614237,2 5.5,2 Z M7.5,2 C7.22385763,2 7,1.77614237 7,1.5 C7,1.22385763 7.22385763,1 7.5,1 C7.77614237,1 8,1.22385763 8,1.5 C8,1.77614237 7.77614237,2 7.5,2 Z M2.5,3 C2.22385763,3 2,2.77614237 2,2.5 C2,2.22385763 2.22385763,2 2.5,2 C2.77614237,2 3,2.22385763 3,2.5 C3,2.77614237 2.77614237,3 2.5,3 Z M4.5,3 C4.22385763,3 4,2.77614237 4,2.5 C4,2.22385763 4.22385763,2 4.5,2 C4.77614237,2 5,2.22385763 5,2.5 C5,2.77614237 4.77614237,3 4.5,3 Z M6.5,3 C6.22385763,3 6,2.77614237 6,2.5 C6,2.22385763 6.22385763,2 6.5,2 C6.77614237,2 7,2.22385763 7,2.5 C7,2.77614237 6.77614237,3 6.5,3 Z M7.5,4 C7.22385763,4 7,3.77614237 7,3.5 C7,3.22385763 7.22385763,3 7.5,3 C7.77614237,3 8,3.22385763 8,3.5 C8,3.77614237 7.77614237,4 7.5,4 Z M5.5,4 C5.22385763,4 5,3.77614237 5,3.5 C5,3.22385763 5.22385763,3 5.5,3 C5.77614237,3 6,3.22385763 6,3.5 C6,3.77614237 5.77614237,4 5.5,4 Z M3.5,4 C3.22385763,4 3,3.77614237 3,3.5 C3,3.22385763 3.22385763,3 3.5,3 C3.77614237,3 4,3.22385763 4,3.5 C4,3.77614237 3.77614237,4 3.5,4 Z M1.5,4 C1.22385763,4 1,3.77614237 1,3.5 C1,3.22385763 1.22385763,3 1.5,3 C1.77614237,3 2,3.22385763 2,3.5 C2,3.77614237 1.77614237,4 1.5,4 Z M2.5,5 C2.22385763,5 2,4.77614237 2,4.5 C2,4.22385763 2.22385763,4 2.5,4 C2.77614237,4 3,4.22385763 3,4.5 C3,4.77614237 2.77614237,5 2.5,5 Z M4.5,5 C4.22385763,5 4,4.77614237 4,4.5 C4,4.22385763 4.22385763,4 4.5,4 C4.77614237,4 5,4.22385763 5,4.5 C5,4.77614237 4.77614237,5 4.5,5 Z M6.5,5 C6.22385763,5 6,4.77614237 6,4.5 C6,4.22385763 6.22385763,4 6.5,4 C6.77614237,4 7,4.22385763 7,4.5 C7,4.77614237 6.77614237,5 6.5,5 Z M7.5,6 C7.22385763,6 7,5.77614237 7,5.5 C7,5.22385763 7.22385763,5 7.5,5 C7.77614237,5 8,5.22385763 8,5.5 C8,5.77614237 7.77614237,6 7.5,6 Z M5.5,6 C5.22385763,6 5,5.77614237 5,5.5 C5,5.22385763 5.22385763,5 5.5,5 C5.77614237,5 6,5.22385763 6,5.5 C6,5.77614237 5.77614237,6 5.5,6 Z M3.5,6 C3.22385763,6 3,5.77614237 3,5.5 C3,5.22385763 3.22385763,5 3.5,5 C3.77614237,5 4,5.22385763 4,5.5 C4,5.77614237 3.77614237,6 3.5,6 Z M1.5,6 C1.22385763,6 1,5.77614237 1,5.5 C1,5.22385763 1.22385763,5 1.5,5 C1.77614237,5 2,5.22385763 2,5.5 C2,5.77614237 1.77614237,6 1.5,6 Z\" id=\"Oval-43\" fill=\"url(#linearGradient-1)\"/>\\n        </g>\\n    </g>\\n</svg>";
        var svgMd5 = EncryptionHelper.MD5Encrypt32(svg);
        var client = new AwsS3Client(_awsS3Option.CurrentValue);
        var byteData = Encoding.UTF8.GetBytes(svg);
        var res= await client.UpLoadFileAsync(new MemoryStream(byteData), svgMd5);
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GETImageProcessProvider());
    }

    protected IImageProcessProvider GETImageProcessProvider()
    {
        var mockImageClient = new Mock<IImageProcessProvider>();
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"21px\" height=\"15px\" viewBox=\"0 0 21 15\" version=\"1.1\">\\n    <!-- Generator: sketchtool 46 (44423) - http://www.bohemiancoding.com/sketch -->\\n    <title>US</title>\\n    <desc>Created with sketchtool.</desc>\\n    <defs>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-1\">\\n            <stop stop-color=\"#FFFFFF\" offset=\"0%\"/>\\n            <stop stop-color=\"#F0F0F0\" offset=\"100%\"/>\\n        </linearGradient>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-2\">\\n            <stop stop-color=\"#D02F44\" offset=\"0%\"/>\\n            <stop stop-color=\"#B12537\" offset=\"100%\"/>\\n        </linearGradient>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-3\">\\n            <stop stop-color=\"#46467F\" offset=\"0%\"/>\\n            <stop stop-color=\"#3C3C6D\" offset=\"100%\"/>\\n        </linearGradient>\\n    </defs>\\n    <g id=\"Symbols\" stroke=\"none\" stroke-width=\"1\" fill=\"none\" fill-rule=\"evenodd\">\\n        <g id=\"US\">\\n            <rect id=\"FlagBackground\" fill=\"url(#linearGradient-1)\" x=\"0\" y=\"0\" width=\"21\" height=\"15\"/>\\n            <path d=\"M0,0 L21,0 L21,1 L0,1 L0,0 Z M0,2 L21,2 L21,3 L0,3 L0,2 Z M0,4 L21,4 L21,5 L0,5 L0,4 Z M0,6 L21,6 L21,7 L0,7 L0,6 Z M0,8 L21,8 L21,9 L0,9 L0,8 Z M0,10 L21,10 L21,11 L0,11 L0,10 Z M0,12 L21,12 L21,13 L0,13 L0,12 Z M0,14 L21,14 L21,15 L0,15 L0,14 Z\" id=\"Rectangle-511\" fill=\"url(#linearGradient-2)\"/>\\n            <rect id=\"Rectangle-511\" fill=\"url(#linearGradient-3)\" x=\"0\" y=\"0\" width=\"9\" height=\"7\"/>\\n            <path d=\"M1.5,2 C1.22385763,2 1,1.77614237 1,1.5 C1,1.22385763 1.22385763,1 1.5,1 C1.77614237,1 2,1.22385763 2,1.5 C2,1.77614237 1.77614237,2 1.5,2 Z M3.5,2 C3.22385763,2 3,1.77614237 3,1.5 C3,1.22385763 3.22385763,1 3.5,1 C3.77614237,1 4,1.22385763 4,1.5 C4,1.77614237 3.77614237,2 3.5,2 Z M5.5,2 C5.22385763,2 5,1.77614237 5,1.5 C5,1.22385763 5.22385763,1 5.5,1 C5.77614237,1 6,1.22385763 6,1.5 C6,1.77614237 5.77614237,2 5.5,2 Z M7.5,2 C7.22385763,2 7,1.77614237 7,1.5 C7,1.22385763 7.22385763,1 7.5,1 C7.77614237,1 8,1.22385763 8,1.5 C8,1.77614237 7.77614237,2 7.5,2 Z M2.5,3 C2.22385763,3 2,2.77614237 2,2.5 C2,2.22385763 2.22385763,2 2.5,2 C2.77614237,2 3,2.22385763 3,2.5 C3,2.77614237 2.77614237,3 2.5,3 Z M4.5,3 C4.22385763,3 4,2.77614237 4,2.5 C4,2.22385763 4.22385763,2 4.5,2 C4.77614237,2 5,2.22385763 5,2.5 C5,2.77614237 4.77614237,3 4.5,3 Z M6.5,3 C6.22385763,3 6,2.77614237 6,2.5 C6,2.22385763 6.22385763,2 6.5,2 C6.77614237,2 7,2.22385763 7,2.5 C7,2.77614237 6.77614237,3 6.5,3 Z M7.5,4 C7.22385763,4 7,3.77614237 7,3.5 C7,3.22385763 7.22385763,3 7.5,3 C7.77614237,3 8,3.22385763 8,3.5 C8,3.77614237 7.77614237,4 7.5,4 Z M5.5,4 C5.22385763,4 5,3.77614237 5,3.5 C5,3.22385763 5.22385763,3 5.5,3 C5.77614237,3 6,3.22385763 6,3.5 C6,3.77614237 5.77614237,4 5.5,4 Z M3.5,4 C3.22385763,4 3,3.77614237 3,3.5 C3,3.22385763 3.22385763,3 3.5,3 C3.77614237,3 4,3.22385763 4,3.5 C4,3.77614237 3.77614237,4 3.5,4 Z M1.5,4 C1.22385763,4 1,3.77614237 1,3.5 C1,3.22385763 1.22385763,3 1.5,3 C1.77614237,3 2,3.22385763 2,3.5 C2,3.77614237 1.77614237,4 1.5,4 Z M2.5,5 C2.22385763,5 2,4.77614237 2,4.5 C2,4.22385763 2.22385763,4 2.5,4 C2.77614237,4 3,4.22385763 3,4.5 C3,4.77614237 2.77614237,5 2.5,5 Z M4.5,5 C4.22385763,5 4,4.77614237 4,4.5 C4,4.22385763 4.22385763,4 4.5,4 C4.77614237,4 5,4.22385763 5,4.5 C5,4.77614237 4.77614237,5 4.5,5 Z M6.5,5 C6.22385763,5 6,4.77614237 6,4.5 C6,4.22385763 6.22385763,4 6.5,4 C6.77614237,4 7,4.22385763 7,4.5 C7,4.77614237 6.77614237,5 6.5,5 Z M7.5,6 C7.22385763,6 7,5.77614237 7,5.5 C7,5.22385763 7.22385763,5 7.5,5 C7.77614237,5 8,5.22385763 8,5.5 C8,5.77614237 7.77614237,6 7.5,6 Z M5.5,6 C5.22385763,6 5,5.77614237 5,5.5 C5,5.22385763 5.22385763,5 5.5,5 C5.77614237,5 6,5.22385763 6,5.5 C6,5.77614237 5.77614237,6 5.5,6 Z M3.5,6 C3.22385763,6 3,5.77614237 3,5.5 C3,5.22385763 3.22385763,5 3.5,5 C3.77614237,5 4,5.22385763 4,5.5 C4,5.77614237 3.77614237,6 3.5,6 Z M1.5,6 C1.22385763,6 1,5.77614237 1,5.5 C1,5.22385763 1.22385763,5 1.5,5 C1.77614237,5 2,5.22385763 2,5.5 C2,5.77614237 1.77614237,6 1.5,6 Z\" id=\"Oval-43\" fill=\"url(#linearGradient-1)\"/>\\n        </g>\\n    </g>\\n</svg>";
        var svgMd5 = EncryptionHelper.MD5Encrypt32(svg);
        // mockImageClient.Setup(p => p.UploadSvgAsync(svgMd5)).ReturnsAsync("result");
        return mockImageClient.Object;
    }
    [Fact]
public async Task uploadTest()
{

    try
    {

        var svg = "<svg xmlns=\"​http://www.w3.org/2000/svg\" xmlns:xlink=\"​http://www.w3.org/1999/xlink\" width=\"21px\" height=\"15px\" viewBox=\"0 0 21 15\" version=\"1.1\">\\n    <!-- Generator: sketchtool 46 (44423) - http://www.bohemiancoding.com/sketch -->\\n    <title>US</title>\\n    <desc>Created with sketchtool.</desc>\\n    <defs>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-1\">\\n            <stop stop-color=\"#FFFFFF\" offset=\"0%\"/>\\n            <stop stop-color=\"#F0F0F0\" offset=\"100%\"/>\\n        </linearGradient>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-2\">\\n            <stop stop-color=\"#D02F44\" offset=\"0%\"/>\\n            <stop stop-color=\"#B12537\" offset=\"100%\"/>\\n        </linearGradient>\\n        <linearGradient x1=\"50%\" y1=\"0%\" x2=\"50%\" y2=\"100%\" id=\"linearGradient-3\">\\n            <stop stop-color=\"#46467F\" offset=\"0%\"/>\\n            <stop stop-color=\"#3C3C6D\" offset=\"100%\"/>\\n        </linearGradient>\\n    </defs>\\n    <g id=\"Symbols\" stroke=\"none\" stroke-width=\"1\" fill=\"none\" fill-rule=\"evenodd\">\\n        <g id=\"US\">\\n            <rect id=\"FlagBackground\" fill=\"url(#linearGradient-1)\" x=\"0\" y=\"0\" width=\"21\" height=\"15\"/>\\n            <path d=\"M0,0 L21,0 L21,1 L0,1 L0,0 Z M0,2 L21,2 L21,3 L0,3 L0,2 Z M0,4 L21,4 L21,5 L0,5 L0,4 Z M0,6 L21,6 L21,7 L0,7 L0,6 Z M0,8 L21,8 L21,9 L0,9 L0,8 Z M0,10 L21,10 L21,11 L0,11 L0,10 Z M0,12 L21,12 L21,13 L0,13 L0,12 Z M0,14 L21,14 L21,15 L0,15 L0,14 Z\" id=\"Rectangle-511\" fill=\"url(#linearGradient-2)\"/>\\n            <rect id=\"Rectangle-511\" fill=\"url(#linearGradient-3)\" x=\"0\" y=\"0\" width=\"9\" height=\"7\"/>\\n            <path d=\"M1.5,2 C1.22385763,2 1,1.77614237 1,1.5 C1,1.22385763 1.22385763,1 1.5,1 C1.77614237,1 2,1.22385763 2,1.5 C2,1.77614237 1.77614237,2 1.5,2 Z M3.5,2 C3.22385763,2 3,1.77614237 3,1.5 C3,1.22385763 3.22385763,1 3.5,1 C3.77614237,1 4,1.22385763 4,1.5 C4,1.77614237 3.77614237,2 3.5,2 Z M5.5,2 C5.22385763,2 5,1.77614237 5,1.5 C5,1.22385763 5.22385763,1 5.5,1 C5.77614237,1 6,1.22385763 6,1.5 C6,1.77614237 5.77614237,2 5.5,2 Z M7.5,2 C7.22385763,2 7,1.77614237 7,1.5 C7,1.22385763 7.22385763,1 7.5,1 C7.77614237,1 8,1.22385763 8,1.5 C8,1.77614237 7.77614237,2 7.5,2 Z M2.5,3 C2.22385763,3 2,2.77614237 2,2.5 C2,2.22385763 2.22385763,2 2.5,2 C2.77614237,2 3,2.22385763 3,2.5 C3,2.77614237 2.77614237,3 2.5,3 Z M4.5,3 C4.22385763,3 4,2.77614237 4,2.5 C4,2.22385763 4.22385763,2 4.5,2 C4.77614237,2 5,2.22385763 5,2.5 C5,2.77614237 4.77614237,3 4.5,3 Z M6.5,3 C6.22385763,3 6,2.77614237 6,2.5 C6,2.22385763 6.22385763,2 6.5,2 C6.77614237,2 7,2.22385763 7,2.5 C7,2.77614237 6.77614237,3 6.5,3 Z M7.5,4 C7.22385763,4 7,3.77614237 7,3.5 C7,3.22385763 7.22385763,3 7.5,3 C7.77614237,3 8,3.22385763 8,3.5 C8,3.77614237 7.77614237,4 7.5,4 Z M5.5,4 C5.22385763,4 5,3.77614237 5,3.5 C5,3.22385763 5.22385763,3 5.5,3 C5.77614237,3 6,3.22385763 6,3.5 C6,3.77614237 5.77614237,4 5.5,4 Z M3.5,4 C3.22385763,4 3,3.77614237 3,3.5 C3,3.22385763 3.22385763,3 3.5,3 C3.77614237,3 4,3.22385763 4,3.5 C4,3.77614237 3.77614237,4 3.5,4 Z M1.5,4 C1.22385763,4 1,3.77614237 1,3.5 C1,3.22385763 1.22385763,3 1.5,3 C1.77614237,3 2,3.22385763 2,3.5 C2,3.77614237 1.77614237,4 1.5,4 Z M2.5,5 C2.22385763,5 2,4.77614237 2,4.5 C2,4.22385763 2.22385763,4 2.5,4 C2.77614237,4 3,4.22385763 3,4.5 C3,4.77614237 2.77614237,5 2.5,5 Z M4.5,5 C4.22385763,5 4,4.77614237 4,4.5 C4,4.22385763 4.22385763,4 4.5,4 C4.77614237,4 5,4.22385763 5,4.5 C5,4.77614237 4.77614237,5 4.5,5 Z M6.5,5 C6.22385763,5 6,4.77614237 6,4.5 C6,4.22385763 6.22385763,4 6.5,4 C6.77614237,4 7,4.22385763 7,4.5 C7,4.77614237 6.77614237,5 6.5,5 Z M7.5,6 C7.22385763,6 7,5.77614237 7,5.5 C7,5.22385763 7.22385763,5 7.5,5 C7.77614237,5 8,5.22385763 8,5.5 C8,5.77614237 7.77614237,6 7.5,6 Z M5.5,6 C5.22385763,6 5,5.77614237 5,5.5 C5,5.22385763 5.22385763,5 5.5,5 C5.77614237,5 6,5.22385763 6,5.5 C6,5.77614237 5.77614237,6 5.5,6 Z M3.5,6 C3.22385763,6 3,5.77614237 3,5.5 C3,5.22385763 3.22385763,5 3.5,5 C3.77614237,5 4,5.22385763 4,5.5 C4,5.77614237 3.77614237,6 3.5,6 Z M1.5,6 C1.22385763,6 1,5.77614237 1,5.5 C1,5.22385763 1.22385763,5 1.5,5 C1.77614237,5 2,5.22385763 2,5.5 C2,5.77614237 1.77614237,6 1.5,6 Z\" id=\"Oval-43\" fill=\"url(#linearGradient-1)\"/>\\n        </g>\\n    </g>\\n</svg>";
        var byteData = Encoding.UTF8.GetBytes(svg);
        var identityPoolId = "ap-northeast-1:709f6fa2-6d5d-497d-94ae-17e85e500d16";

        var cognitoCredentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.APNortheast1);

        using (var s3Client = new AmazonS3Client(cognitoCredentials, RegionEndpoint.APNortheast1))
        {
            var filePath = "image/svg/test.svg";
            var bucketName = "portkey-im-dev";

            try
            {
                var putObjectRequest = new PutObjectRequest
                {
                    InputStream = new MemoryStream(byteData),
                    BucketName = bucketName,
                    Key = "soho-01/images/svg/test.svg",
                    CannedACL = S3CannedACL.PublicRead,
                };
                var res = await s3Client.PutObjectAsync(putObjectRequest);
                _testOutputHelper.WriteLine("File uploaded successfully.");
            }
            catch (AmazonS3Exception e)
            {
                _testOutputHelper.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                _testOutputHelper.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }
    }
    catch (Exception e)
    {
        _testOutputHelper.WriteLine(e.Message);
    }

}
}