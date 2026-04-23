using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Web.Services.Interfaces;

public interface IExternalLoginStore
{
    Task CreateAsyncLoginInfo(Guid userId, string provider, string subject, string? email);
}
