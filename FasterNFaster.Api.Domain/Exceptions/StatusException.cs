namespace FasterNFaster.Api.Core.Exceptions;

public class StatusException(string message) : Exception(message)
{
    public virtual int StatusCode => 500;

    public virtual string? Code => null;

    public virtual IReadOnlyDictionary<string, object?>? Extensions => null;
}
