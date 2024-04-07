using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;

namespace CAServer.IpInfo;

[RemoteService(false), DisableAuditing]
public class IpInfoAppService : CAServerAppService, IIpInfoAppService
{
    private readonly IIpInfoClient _ipInfoClient;
    private readonly DefaultIpInfoOptions _defaultIpInfoOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDistributedCache<IpInfoResultDto> _distributedCache;
    private readonly IpServiceSettingOptions _ipServiceSettingOptions;
    private readonly IHttpClientService _httpClientService;
    private readonly string _prefix = "IpInfo-";

    private readonly string _ipPattern =
        @"^([0,1]?\d{1,2}|2([0-4][0-9]|5[0-5]))(\.([0,1]?\d{1,2}|2([0-4][0-9]|5[0-5]))){3}$";

    public IpInfoAppService(IIpInfoClient ipInfoClient,
        IHttpContextAccessor httpContextAccessor,
        IOptions<DefaultIpInfoOptions> defaultIpInfoOptions,
        IOptions<IpServiceSettingOptions> ipServiceSettingOptions,
        IDistributedCache<IpInfoResultDto> distributedCache, IHttpClientService httpClientService)
    {
        _ipInfoClient = ipInfoClient;
        _httpContextAccessor = httpContextAccessor;
        _distributedCache = distributedCache;
        _httpClientService = httpClientService;
        _defaultIpInfoOptions = defaultIpInfoOptions.Value;
        _ipServiceSettingOptions = ipServiceSettingOptions.Value;
    }

    public async Task<IpInfoResultDto> GetIpInfoAsync()
    {
        var ip = string.Empty;
        try
        {
            ip = GetIp();
            var ipInfoResult = await _distributedCache.GetAsync(_prefix + ip);

            if (ipInfoResult != null) return ipInfoResult;

            var ipInfoDto = await _ipInfoClient.GetIpInfoAsync(ip);
            var ipInfo = ObjectMapper.Map<IpInfoDto, IpInfoResultDto>(ipInfoDto);

            if (string.IsNullOrEmpty(ipInfo.Country))
            {
                return ObjectMapper.Map<DefaultIpInfoOptions, IpInfoResultDto>(_defaultIpInfoOptions);
            }

            await _distributedCache.SetAsync(_prefix + ip, ipInfo, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = CommonConstant.DefaultAbsoluteExpiration
            });
            return ipInfo;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Get ipInfo fail, ip:{ip}", ip);
            return ObjectMapper.Map<DefaultIpInfoOptions, IpInfoResultDto>(_defaultIpInfoOptions);
        }
    }

    public async Task UpdateRepairScore()
    {
        GetRepair();

        var header = new Dictionary<string, string>()
        {
            ["Authorization"] =
                "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IjZDREQxMUUwNThDRjk4NDY3QjM0QzJGNEFGMTZCMzBDMzc5NDkxNzciLCJ4NXQiOiJiTjBSNEZqUG1FWjdOTUwwcnhhekREZVVrWGMiLCJ0eXAiOiJhdCtqd3QifQ.eyJzdWIiOiI1OTEyYzFlNS0zNDRjLWM0OWUtNTM2MS0zYTExMmZmMDc0ZjAiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AYWJwLmlvIiwicm9sZSI6ImFkbWluIiwiZ2l2ZW5fbmFtZSI6ImFkbWluIiwicGhvbmVfbnVtYmVyX3ZlcmlmaWVkIjoiRmFsc2UiLCJlbWFpbF92ZXJpZmllZCI6IkZhbHNlIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsIm9pX3Byc3QiOiJTY2hyb2RpbmdlclNlcnZlcl9BcHAiLCJjbGllbnRfaWQiOiJTY2hyb2RpbmdlclNlcnZlcl9BcHAiLCJvaV90a25faWQiOiJhMjdlZGIzZS1lYmIxLTRiZDQtNzcyNy0zYTExODZlMjIwYTMiLCJhdWQiOiJTY2hyb2RpbmdlclNlcnZlciIsInNjb3BlIjoiU2Nocm9kaW5nZXJTZXJ2ZXIiLCJqdGkiOiJkNTc3NjIwNi0yYTc3LTQ5ODAtOTFmNS01N2UxZmU4MzExNjEiLCJleHAiOjE3MTE1Mjc4MjAsImlzcyI6Imh0dHA6Ly9sb2NhbGhvc3Q6ODA4MC8iLCJpYXQiOjE3MTEzNTUwMjF9.akPQ8l4emxLD7Kf_-Ztz5sts2xptEIdprUZQpTEgaJe5o7cYxHEudifqkPJU0jqVah5sr37By3U4YCkMAghWIGw3Nyt3_jFG0r8EXmDb_tBf39dk87hpyBpYn9phusqsZhABos1S3-RUGfNNklJ02nxwvaUgsE-qTOlBl-OLlq4a1OpwwA6RwfwKrnLeDfBiP4Mz7gTSWwzkYngY2r69FrqPsbnl_-2F83CkovbfnZnBHSGncHqHmIrEVf-BQxnRnHEGCkx0Ckorc9AW7CN0_8pUJr2TZhtl7ghONorSkRNxQnO4LU9DrP-e1-81zbVmESoZb-dgHJqRsxQl1kzNkA"
        };
        await _httpClientService.PostJsonAsync("https://schrodingernft.ai/api/app/repair/xp-score", _repairScore, header);
    }

    private List<XpScoreRepairDataDto> _rawScore = new List<XpScoreRepairDataDto>();
    private List<XpScoreRepairDataDto> _actuScore = new List<XpScoreRepairDataDto>();
    private List<UpdateXpScoreRepairDataDto> _repairScore = new List<UpdateXpScoreRepairDataDto>();

    public async Task<List<UpdateXpScoreRepairDataDto>> GetIpInfo2Async()
    {
        GetRaw();
        GetActu();

        foreach (var per in _rawScore)
        {
            var actu = _actuScore.FirstOrDefault(t => t.UserId == per.UserId);
            if (actu == null)
            {
                Console.WriteLine($"actu is null, uid:{per.UserId}");
                continue;
            }

            if (actu.Score - per.Score != 0)
            {
                _repairScore.Add(new UpdateXpScoreRepairDataDto()
                {
                    UserId = per.UserId,
                    ActualScore = actu.Score,
                    RawScore = per.Score
                });
            }
        }

        Console.WriteLine($"repair count:{_repairScore.Count}");
        WriteScore();

        return new List<UpdateXpScoreRepairDataDto>();
    }

    private void GetRaw()
    {
        var sr = new StreamReader(@"score_raw.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            var aaa = nextLine.Split('\t');
            _rawScore.Add(new XpScoreRepairDataDto()
            {
                UserId = aaa[0],
                Score = Convert.ToInt32(aaa[1])
            });
        }

        sr.Close();
    }

    private void GetActu()
    {
        var sr = new StreamReader(@"score_actu.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            var aaa = nextLine.Split('\t');
            _actuScore.Add(new XpScoreRepairDataDto()
            {
                UserId = aaa[0].Trim(),
                Score = Convert.ToInt32(aaa[1].Trim())
            });
        }

        sr.Close();
    }

    private void GetRepair()
    {
        var sr = new StreamReader(@"score_repair.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            var aaa = nextLine.Split('\t');
            _repairScore.Add(new UpdateXpScoreRepairDataDto()
            {
                UserId = aaa[0],
                RawScore = Convert.ToInt32(aaa[1]),
                ActualScore = Convert.ToInt32(aaa[2])
            });
        }

        sr.Close();
    }

    private void WriteScore()
    {
        var fileInfo = new FileInfo(@"score_repair.txt");
        var sw = fileInfo.CreateText();
        foreach (var address in _repairScore)
        {
            sw.WriteLine($"{address.UserId}\t{address.RawScore}\t{address.ActualScore}");
        }

        sw.Flush();
        sw.Close();
    }


    private string GetIp()
    {
        if (_httpContextAccessor?.HttpContext?.Request.Headers?.ContainsKey("X-Forwarded-For") == false)
        {
            throw new UserFriendlyException("Not set nginx header.");
        }

        Logger.LogInformation(
            $"X-Forwarded-For: {_httpContextAccessor?.HttpContext?.Request.Headers["X-Forwarded-For"].ToString()}");

        var ip = _httpContextAccessor?.HttpContext?.Request.Headers["X-Forwarded-For"].ToString().Split(',')
            .FirstOrDefault();

        ip ??= string.Empty;

        if (!Match(ip))
        {
            throw new UserFriendlyException("Unknown ip address: {ip}.", ip);
        }

        return ip;
    }

    private bool Match(string ip) => new Regex(_ipPattern).IsMatch(ip);
}