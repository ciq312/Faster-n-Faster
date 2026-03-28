namespace FasterNFaster.Api.UseCases.Users.LoginUsers;

public record LoginUserResult(string Token, Guid UserId);