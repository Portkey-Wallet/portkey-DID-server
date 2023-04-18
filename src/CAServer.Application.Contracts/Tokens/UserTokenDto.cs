using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core.Tokenizer;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens;

public class UserTokenDto
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDisplay { get; set; }
    public Token Token { get; set; }
}
