
using FasterNFaster.Api.Core.Exceptions;

namespace FasterNFaster.Api.UseCases.Exceptions;

public class UserNotFoundException : DomainException
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