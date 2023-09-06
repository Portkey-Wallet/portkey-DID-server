using System;
using System.Collections.Generic;
using CAServer.Entities.Es;
using Nest;

namespace CAServer.PrivacyPolicy;

public class PrivacyPolicyQueryBuilder
{
    private List<Func<QueryContainerDescriptor<PrivacyPolicyIndex>, QueryContainer>> _mustQueries;

    public PrivacyPolicyQueryBuilder()
    {
        _mustQueries = new List<Func<QueryContainerDescriptor<PrivacyPolicyIndex>, QueryContainer>>();
    }

    public PrivacyPolicyQueryBuilder WithVersion(int policyVersion)
    {
        if (policyVersion > 0)
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.PolicyVersion).Value(policyVersion)));
        }

        return this;
    }

    public PrivacyPolicyQueryBuilder WithCaHash(string caHash)
    {
        if (!string.IsNullOrWhiteSpace(caHash))
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.CaHash).Value(caHash)));
        }

        return this;
    }

    public PrivacyPolicyQueryBuilder WithOrigin(string origin)
    {
        if (!string.IsNullOrWhiteSpace(origin))
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.Origin).Value(origin)));
        }

        return this;
    }

    public PrivacyPolicyQueryBuilder WithScene(int scene)
    {
        if (scene > 0)
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.Scene).Value(scene)));
        }

        return this;
    }

    public PrivacyPolicyQueryBuilder WithManagerAddress(string managerAddress)
    {
        if (!string.IsNullOrWhiteSpace(managerAddress))
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.ManagerAddress).Value(managerAddress)));
        }

        return this;
    }
    
    public List<Func<QueryContainerDescriptor<PrivacyPolicyIndex>, QueryContainer>> Build()
    {
        return _mustQueries;
    }
}