using System.Threading.Tasks;
using CAServer.Vote;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Vote")]
[Route("api/app/vote")]
public class VoteController : CAServerController
{
    private readonly IVoteAppService _voteAppService;

    public VoteController(IVoteAppService voteAppService)
    {
        _voteAppService = voteAppService;
    }

    [HttpGet("vote")]
    public async Task<string> GetVote()
    {
        await _voteAppService.GetVote();
        return "ok";
    }
    
    [HttpGet("beauty")]
    public async Task<string> Beauty()
    {
        await _voteAppService.Beauty();
        return "ok";
    }
}