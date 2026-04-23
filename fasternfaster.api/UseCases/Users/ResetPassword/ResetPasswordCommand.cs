namespace FasterNFaster.Api.UseCases.Users.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword);
