using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using GraphQL;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.AddressExtraInfo;

[RemoteService(false), DisableAuditing]
public class AddressExtraInfoService : CAServerAppService, IAddressExtraInfoService
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IHttpClientService _httpClientService;

    public AddressExtraInfoService(IHttpClientService httpClientService, IGraphQLHelper graphQlHelper)
    {
        _httpClientService = httpClientService;
        _graphQlHelper = graphQlHelper;
    }

    private List<string> Addresses = new List<string>();
    private List<AddressInfo> AddressInfos = new List<AddressInfo>();

    public async Task<string> GetLoinInAccount()
    {
        ReadNotMintAddressesInfo();

        var holders = await GetHolderInfoAsync("tDVV", string.Empty, Addresses, 0, 1000);

        foreach (var guardianDto in holders.CaHolderInfo)
        {
            var originChainId = guardianDto.OriginChainId;
            if (originChainId.IsNullOrEmpty())
            {
                originChainId = (guardianDto.GuardianList == null || guardianDto.GuardianList.Guardians.IsNullOrEmpty())
                    ? "AELF"
                    : guardianDto.ChainId;
            }

            var add = new AddressInfo
            {
                OriginChainId = originChainId,
                CaAddress = guardianDto.CaAddress,
                CaHash = guardianDto.CaHash
            };
            AddressInfos.Add(add);
        }

        await DoSth();
        WriteMintAddress();

        return "ok";
    }

    private async Task DoSth()
    {
        foreach (var addressInfo in AddressInfos)
        {
            //get from i
            var url =
                $"https://did-portkey.portkey.finance/api/app/account/guardianIdentifiers?ChainId={addressInfo.OriginChainId}&CaHash={addressInfo.CaHash}";

            var data = await _httpClientService.GetAsync<GuardianResultDto>(url);
            var guardians = data?.GuardianList?.Guardians?.Where(t => t.IsLoginGuardian).ToList();
            var identifiers = new List<string>();
            foreach (var guardian in guardians)
            {
                if (guardian.Type == GuardianIdentifierType.Apple.ToString() ||
                    guardian.Type == GuardianIdentifierType.Google.ToString())
                {
                    identifiers.Add(guardian.ThirdPartyEmail ?? "-");
                }
                else
                {
                    identifiers.Add(guardian.GuardianIdentifier);
                }
            }

            addressInfo.Identifiers = identifiers;
        }
    }


    private void ReadNotMintAddressesInfo()
    {
        var sr = new StreamReader(@"AddressCheck/erlun.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            nextLine = nextLine.Replace("ELF_", "").Replace("_tDVV", "");
            if (!Addresses.Contains(nextLine))
            {
                Addresses.Add(nextLine);
            }
        }

        sr.Close();
    }

    private void WriteMintAddress()
    {
        var fileInfo = new FileInfo(@"AddressCheck/erlun_tdvv.txt");
        var sw = fileInfo.CreateText();
        foreach (var address in AddressInfos)
        {
            string identifiers = "";
            if (address.Identifiers.IsNullOrEmpty())
            {
                identifiers = "-";
            }

            foreach (var identifier in address.Identifiers)
            {
                identifiers = identifiers + ", " + identifier;
            }

            identifiers.TrimStart(',');
            sw.WriteLine($"{address.OriginChainId}\t{address.CaHash}\t{address.CaAddress}\t{identifiers}");
        }

        sw.Flush();
        sw.Close();
    }

    private async Task<GuardiansDto> GetHolderInfoAsync(string chainId, string caHash, List<string> caAddresses,
        int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
            query($chainId:String,$caHash:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
            caHolderInfo(dto: {chainId:$chainId,caHash:$caHash,caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
        }",
            Variables = new
            {
                chainId, caHash, caAddresses, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount
            }
        });
    }
}