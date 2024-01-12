using System.Threading.Tasks;

namespace CAServer.Vote;

public interface IVoteAppService
{
    Task<string> GetVote();
    Task<string> Beauty();
}