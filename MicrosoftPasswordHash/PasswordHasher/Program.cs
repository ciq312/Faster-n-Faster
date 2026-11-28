using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;

var nick = "nick";
var login = "login";
var password = "12345678";

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--nick": nick = NextArg(args, ref i); break;
        case "--password": password = NextArg(args, ref i); break;
        case "--login": login = NextArg(args, ref i); break;
        default: throw new ArgumentException($"Unknown argument: {args[i]}");
    }
}
var user = new User(nick, login, password);
user.SetEmail("alice@example.test");

var passwordHelper = new PasswordHelper(new PasswordHasher<User>());
var hash = passwordHelper.HashPassword(user, "E2e-Racer-2026!");

Console.WriteLine(hash);

static string NextArg(string[] allArgs, ref int index)
{
    if (++index >= allArgs.Length) throw new ArgumentException($"Missing value after {allArgs[index - 1]}.");
    return allArgs[index];
}
