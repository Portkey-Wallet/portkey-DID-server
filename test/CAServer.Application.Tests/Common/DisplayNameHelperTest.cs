using System.Collections.Generic;
using CAServer.Commons;
using Shouldly;
using Xunit;

namespace CAServer.Common;

public class DisplayNameHelperTest
{
    [Fact]
    public void TestDisplayName()
    {
        {
            var dto = new DTOA
            {
                ChainId = "AELF"
            };
            ChainDisplayNameHelper.SetDisplayName(dto);
            dto.DisplayChainName.ShouldBe(ChainDisplayNameHelper.MainChain);
        }
        {
            List<DTOA> dtos = new List<DTOA>
            {
                new DTOA
                {
                    ChainId = "AELF"
                },
            };
            
            ChainDisplayNameHelper.SetDisplayName(dtos);
        }
        {
            var dto = new DTOB
            {
                ChainId = "AELF"
            };
            ChainDisplayNameHelper.SetDisplayName(dto);
        }
        {
            var dto = new DTOC();
            ChainDisplayNameHelper.SetDisplayName(dto);
        }
    }

    public class DTOA
    {
        public string ChainId { get; set; }
        public string DisplayChainName { get; set; }
    }

    public class DTOB
    {
        public string ChainId { get; set; }
    }

    public class DTOC
    {
    }
}