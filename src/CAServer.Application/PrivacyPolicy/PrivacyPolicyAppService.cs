using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.PrivacyPolicy.Dtos;
using Nest;

namespace CAServer.PrivacyPolicy;

public class PrivacyPolicyAppService :CAServerAppService, IPrivacyPolicyAppService
{
    private readonly INESTRepository<PrivacyPolicyIndex, string> _policyIndexRepository;
    
    public PrivacyPolicyAppService(INESTRepository<PrivacyPolicyIndex, string> policyIndexRepository)
    {
        _policyIndexRepository = policyIndexRepository;
    }
    
    public async Task SignAsync(PrivacyPolicySignDto input)
    {
        var result = await GetPrivacyPolicyAsync(new PrivacyPolicyInputDto()
        {
            CaHash = input.CaHash,
            PolicyId = input.PolicyId,
            PolicyVersion = input.PolicyVersion,
            Scene = input.Scene,
            ManagerAddress = input.ManagerAddress,
        });
        
        if (result != null)
        {
            result.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _policyIndexRepository.AddOrUpdateAsync(ObjectMapper.Map<PrivacyPolicyDto, PrivacyPolicyIndex>(result));
            return;
        }
        
        var policyDto = ObjectMapper.Map<PrivacyPolicySignDto, PrivacyPolicyDto>(input);
        policyDto.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        policyDto.Id = Guid.NewGuid().ToString();
        await _policyIndexRepository.AddOrUpdateAsync(ObjectMapper.Map<PrivacyPolicyDto, PrivacyPolicyIndex>(policyDto));
        return;
    }
    
    public async Task<PrivacyPolicyDto> GetPrivacyPolicyAsync(PrivacyPolicyInputDto input)
    {
        var queryBuilder = new PrivacyPolicyQueryBuilder()
            .WithVersion(input.PolicyVersion)
            .WithCaHash(input.CaHash)
            .WithOrigin(input.Origin)
            .WithScene(input.Scene)
            .WithManagerAddress(input.ManagerAddress)
            .Build();
        QueryContainer Filter(QueryContainerDescriptor<PrivacyPolicyIndex> f) => f.Bool(b => b.Must(queryBuilder));

        var list = await _policyIndexRepository.GetListAsync(Filter);
        if (list.Item1 == 0)
        {
            return null;
        }
        
        return ObjectMapper.Map<PrivacyPolicyIndex, PrivacyPolicyDto>(list.Item2[0]);
    }
}