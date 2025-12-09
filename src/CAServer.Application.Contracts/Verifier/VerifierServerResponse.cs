using System;
using System.Collections.Generic;

namespace CAServer.Dtos;

public class VerifierServerResponse
{
    //VerifierSessionId
    public Guid VerifierSessionId { get; set;}

    public string VerifierServerEndpoint { get; set; }
}