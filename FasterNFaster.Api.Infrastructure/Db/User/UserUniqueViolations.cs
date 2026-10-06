using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FasterNFaster.Api.Infrastructure.Db.Users;

public static class UserUniqueViolations
{
    public const string EmailIndex = "IX_Users_Email";
    public const string LoginIndex = "IX_Users_Login";

    private const string UniqueViolationSqlState = "23505";

    public static ConflictException? Translate(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException { SqlState: UniqueViolationSqlState } postgres) return null;

        var user = exception.Entries.Select(entry => entry.Entity).OfType<User>().FirstOrDefault();
        if (user is null) return null;

        return postgres.ConstraintName switch
        {
            EmailIndex when user.Email is { } email => new DuplicateEmailException(email),
            LoginIndex when user.Login is { } login => new DuplicateLoginException(login),
            _ => null
        };
    }
}
