using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;

public interface IGraphQLHelper
{
    Task<T> QueryAsync<T>(GraphQLRequest request);
}

public class GraphQLHelper : IGraphQLHelper, ISingletonDependency
{
    private readonly IGraphQLClient _client;
    private readonly ILogger<GraphQLHelper> _logger;

    public GraphQLHelper(IGraphQLClient client, ILogger<GraphQLHelper> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<T> QueryAsync<T>(GraphQLRequest request)
    {
        var graphQlResponse = await _client.SendQueryAsync<T>(request);
        if (graphQlResponse.Errors is not { Length: > 0 })
        {
            return graphQlResponse.Data;
        }

        _logger.LogError("query graphQL err, errors = {Errors}", string.Join(",", graphQlResponse.Errors.Select(e => e.Message).ToList()));
        return default;
    }
}

public class GraphQLResponseException : Exception
{
    public GraphQLResponseException(string message) : base(message)
    {
    }
}