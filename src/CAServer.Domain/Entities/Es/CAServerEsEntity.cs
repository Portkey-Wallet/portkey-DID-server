using Volo.Abp.Domain.Entities;

namespace CAServer.Entities.Es;

public abstract class CAServerEsEntity<TKey> : Entity, IEntity<TKey>
{
    public virtual TKey Id { get; set; }

    public override object[] GetKeys()
    {
        return new object[] { Id };
    }
}