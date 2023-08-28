using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartOrderProcessor
{
    
    public string ThirdPartName();
    
    public Task UpdateOrderAsync(IThirdOrderUpdateRequest input);

}