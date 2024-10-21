using System;
using TonLibDotNet;
using TonLibDotNet.Cells;
using TonProof.Types;
using InitialAccountState = TonLibDotNet.Types.Raw.InitialAccountState;

namespace CAServer.CAAccount.TonWallet;

public sealed class CheckProofRequestRaw : CheckProofRequest
{
    #region Constructors

    public CheckProofRequestRaw(CheckProofRequest request)
    {
        this.Address = request.Address;
        this.Network = request.Network;
        this.PublicKey = request.PublicKey;
        this.Proof = request.Proof;

        var addressSpan = this.Address.AsSpan();
        if (addressSpan.Length > 2 &&
            addressSpan[1].Equals(':') &&
            uint.TryParse(addressSpan[..1], out var wc))
        {
            this.Workchain = wc;
            this.AddressBytes = Convert.FromHexString(addressSpan[2..]);
        }

        if (Boc.TryParseFromBase64(request.Proof.StateInit, out var boc))
        {
            if (boc.RootCells[0].Refs.Count >= 2)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (boc.RootCells[0].Refs[0] is not null &&
                    boc.RootCells[0].Refs[1] is not null)
                {
                    this.Data = boc.RootCells[0].Refs[1];
                    this.InitState = new InitialAccountState()
                    {
                        Code = boc.RootCells[0].Refs[0].ToBoc().SerializeToBase64(),
                        Data = boc.RootCells[0].Refs[1].ToBoc().SerializeToBase64()
                    };
                }
            }
        }
    }

    #endregion

    #region Public Fields

    public byte[] AddressBytes { get; private set; }

    public uint? Workchain { get; private set; }

    public InitialAccountState InitState { get; private set; }

    public Cell Data { get; private set; }

    #endregion
}