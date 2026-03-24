using FasterNFaster.Api.Core.Entities;

public class UserNotFoundException : System.Exception
{
    public Guid UserId { get; }

    public UserNotFoundException(Guid userId)
        : base($"User with id {userId} not found")
    {
        UserId = userId;
    }
    public UserNotFoundException(Guid userId, string source)
    : base($"User with id {userId} was not found in {source}")
    {
        UserId = userId;
    }
    public UserNotFoundException(string message) : base(message)
    {

    }
}