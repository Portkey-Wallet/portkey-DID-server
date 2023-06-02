using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;

namespace CAServer.ThirdPart;

public interface IAlchemyOrderAppService
{
    /// <summary>
    /// Validate the signature of Alchemy and convert it to the specified class specified by the caller.
    /// The inputDict variable should include a “signature” attribute.
    /// </summary>
    /// <returns>A class T object.</returns>
    /// <exception cref="UserFriendlyException">Thrown when the signature validation fails.</exception>
    Task<T> VerifyAlchemySignature<T>(Dictionary<string, string> inputDict);

    Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input);

    Task UpdateAlchemyTxHashAsync(UpdateAlchemyTxHashDto input);
}