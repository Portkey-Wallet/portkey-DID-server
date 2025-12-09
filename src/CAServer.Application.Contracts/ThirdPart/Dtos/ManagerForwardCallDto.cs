using System;
using System.Text;
using AElf;
using AElf.Types;
using Google.Protobuf;
using Portkey.Contracts.CA;
using Volo.Abp;

namespace CAServer.ThirdPart.Dtos;

public class ManagerForwardCallDto<T> where T : IMessage<T>
{
    private const string ManagerForwardCallMethodName = "ManagerForwardCall";
    public Hash CaHash { get; set; }
    public Address ContractAddress { get; set; }
    public string MethodName { get; set; }
    public Args<T> ForwardTransactionArgs { get; set; }

    
    public static ManagerForwardCallDto<T> Decode(Transaction transaction)
    {
        try
        {
            if (transaction == null || !transaction.MethodName.Equals(ManagerForwardCallMethodName))
                throw new Exception("NOT ManagerForwardCall transaction");

            var param = ManagerForwardCallInput.Parser.ParseFrom(transaction.Params);
            var forwardCallDto = new ManagerForwardCallDto<T>
            {
                MethodName = param.MethodName,
                CaHash = param.CaHash,
                ContractAddress = param.ContractAddress
            };

            var parser = typeof(T).GetProperty("Parser")!.GetValue(null, null) as MessageParser<T>;
            var args = parser!.ParseFrom(param.Args);
            forwardCallDto.ForwardTransactionArgs = new Args<T> { Value = args };

            return forwardCallDto;
        }
        catch (Exception e)
        {
            throw new UserFriendlyException("Convert rawTransaction FAILED!", innerException:e);
        }
    }
}

public class Args<T> where T : IMessage<T>
{
    public T Value { get; set; }
}