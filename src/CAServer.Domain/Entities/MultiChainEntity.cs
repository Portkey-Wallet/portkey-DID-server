namespace CAServer.Entities;

public class MultiChainEntity<TKey> : CAServerEntity<TKey>, IMultiChain
{
    public virtual int ChainId { get; set; }


    protected MultiChainEntity()
    {
    }

    protected MultiChainEntity(TKey id)
        : base(id)
    {
    }
}