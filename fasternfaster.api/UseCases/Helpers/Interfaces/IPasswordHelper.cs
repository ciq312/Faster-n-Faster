using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.UseCases.Helpers.Interfaces;

public interface IPasswordHelper
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string hashedPassword, string password);
}