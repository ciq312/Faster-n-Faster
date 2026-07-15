using FasterNFaster.Api.UseCases.Interfaces.Auth;

namespace FasterNFaster.IntegrationTests;

public class FakeEmailSender : IEmailSender
{
    public Task SendConfirmationEmail(string receiverName, string receiverEmailAddress, string verificationToken) =>
        Task.CompletedTask;

    public Task SendPasswordResetEmail(string receiverName, string receiverEmailAddress, string resetToken) =>
        Task.CompletedTask;
}
