using System;
using System.Threading.Tasks;
using CAServer.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace CAServer.Controllers
{
    [RemoteService]
    [ControllerName("UserToken")]
    [Route("api/app/userTokens")]
    public class UserTokenController : CAServerController
    {
        private readonly IUserTokenAppService _userTokenAppService;

        public UserTokenController(IUserTokenAppService userTokenService)
        {
            _userTokenAppService = userTokenService;
        }

        [HttpGet]
        [Authorize]
        public async Task<PagedResultDto<UserTokenDto>> GetUserTokenList(GetUserTokenListInput input)
        {
            return await _userTokenAppService.GetUserTokenListAsync(input);
        }

        [HttpPut]
        [Route("/{id}/display")]
        [Authorize]
        public async Task ChangeTokenDisplayAsync(Guid id,bool isDisplay)
        {
            await _userTokenAppService.ChangeTokenDisplayAsync(isDisplay, id);
        }

        [HttpPost]
        [Route("/add")]
        public async Task AddToken()
        {
            await _userTokenAppService.AddUserTokenAsync(new Guid());
        }
    }
}