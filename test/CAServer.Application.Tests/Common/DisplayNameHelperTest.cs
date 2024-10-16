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
            bool result = ChainDisplayNameHelper.SetDisplayName(dto);
            result.ShouldBeTrue();
            dto.DisplayName.ShouldBe(ChainDisplayNameHelper.MainChain);
        }
        {
            List<DTOA> dtos = new List<DTOA>
            {
                new DTOA
                {
                    ChainId = "AELF"
                },
            };
            
            bool result = ChainDisplayNameHelper.SetDisplayName(dtos);
            result.ShouldBeTrue();
        }
        {
            var dto = new DTOB
            {
                ChainId = "AELF"
            };
            bool result = ChainDisplayNameHelper.SetDisplayName(dto);
            result.ShouldBeFalse();
        }
        {
            var dto = new DTOC();
            bool result = ChainDisplayNameHelper.SetDisplayName(dto);
            result.ShouldBeFalse();
        }
    }

    public class DTOA
    {
        public string ChainId { get; set; }
        public string DisplayName { get; set; }
    }

    public class DTOB
    {
        public string ChainId { get; set; }
    }

    public class DTOC
    {
    }
}