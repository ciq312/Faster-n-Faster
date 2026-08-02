namespace FasterNFaster.Api.Core;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }

    public override bool Equals(object? obj) => obj is Entity<TId> entity && GetType() != entity.GetType() && Id is not null && Id.Equals(entity.Id);

    public override int GetHashCode() => Id?.GetHashCode() ?? 0;
}