using System;
using CAServer.ContractEventHandler.Core.Application;
using Xunit;

namespace CAServer.ApplicationHandler;

public class ApplicationHandlerTest : CAServerApplicationTestBase
{
    private readonly IContractAppService _contractAppService;

    public ApplicationHandlerTest()
    {
        _contractAppService = GetRequiredService<IContractAppService>();
    }
}