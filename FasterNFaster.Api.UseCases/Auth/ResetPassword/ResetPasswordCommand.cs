using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
