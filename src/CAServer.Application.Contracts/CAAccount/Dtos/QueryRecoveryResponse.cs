using System;
namespace CAServer.Dtos
{
    public class QueryRecoveryResponse
    {

        public string RecoveryStatus { get; set; }
        public string RecoveryMessage { get; set; }
        public string CaHash { get; set; }
        public string CaAddress { get; set; }

    }
}

