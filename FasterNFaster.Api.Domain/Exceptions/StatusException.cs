namespace FasterNFaster.Api.Domain.Exceptions;

public class StatusException(string message) : Exception(message)
{
    public virtual string? Code => null;

    public virtual IReadOnlyDictionary<string, object?>? Extensions => null;
}
