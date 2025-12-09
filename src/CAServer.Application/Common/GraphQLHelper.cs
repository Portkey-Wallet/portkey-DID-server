using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Monitor;
using CAServer.Monitor.Logger;
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
    private readonly IIndicatorLogger _indicatorLogger;

    public GraphQLHelper(IGraphQLClient client, ILogger<GraphQLHelper> logger, IIndicatorLogger indicatorLogger)
    {
        _client = client;
        _logger = logger;
        _indicatorLogger = indicatorLogger;
    }
    
    public async Task<T> QueryAsync<T>(GraphQLRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await SendQueryAsync<T>(request);
        stopwatch.Stop();

        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = $"{nameof(QueryAsync)}:{typeof(T).Name}";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        return response;
    }

    private async Task<T> SendQueryAsync<T>(GraphQLRequest request)
    {
        var graphQlResponse = await _client.SendQueryAsync<T>(request);
        if (graphQlResponse.Errors is not { Length: > 0 })
        {
            return graphQlResponse.Data;
        }

        _logger.LogError("query graphQL err, errors = {Errors}",
            string.Join(",", graphQlResponse.Errors.Select(e => e.Message).ToList()));
        return default;
    }
}

public class GraphQLResponseException : Exception
{
    public GraphQLResponseException(string message) : base(message)
    {
    }
}