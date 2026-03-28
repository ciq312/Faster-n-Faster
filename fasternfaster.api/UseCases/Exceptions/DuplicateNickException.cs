namespace FasterNFaster.Api.UseCases.Exceptions;

public class DuplicateNickException : Exception
{
    public string Nick { get; }

    public DuplicateNickException(string nick)
        : base($"User with nick '{nick}' already exists")
    {
        Nick = nick;
    }
}
