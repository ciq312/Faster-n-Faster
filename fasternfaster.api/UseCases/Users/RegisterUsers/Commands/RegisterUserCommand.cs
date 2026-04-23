namespace FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;

public record RegisterUserCommand(string Nick, string Login, string Email, string Password);