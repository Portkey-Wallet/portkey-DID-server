using System;
using Volo.Abp.Domain.Entities;

namespace CAServer.Entities;

[Serializable]
public abstract class CAServerEntity <TKey> : Entity, IEntity<TKey>
{
    /// <inheritdoc/>
    public virtual TKey Id { get; set; }

    protected CAServerEntity()
    {

    }

    protected CAServerEntity(TKey id)
    {
        Id = id;
    }

    public override object[] GetKeys()
    {
        return new object[] {Id};
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }
}