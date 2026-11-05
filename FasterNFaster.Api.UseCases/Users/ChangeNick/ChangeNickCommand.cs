using MediatR;

namespace FasterNFaster.Api.UseCases.Users.ChangeNick;

public record ChangeNickCommand(Guid UserId, string Nick) : IRequest<ChangeNickResult>;
