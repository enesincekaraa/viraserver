namespace Vira.Shared.Base;
public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
}
