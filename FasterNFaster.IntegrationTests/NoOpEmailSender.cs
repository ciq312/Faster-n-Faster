using FasterNFaster.Api.UseCases.Auth.Ports;

namespace FasterNFaster.IntegrationTests;

public class NoOpEmailSender : IEmailSender
{
    public Task SendConfirmationEmail(string receiverName, string receiverEmailAddress, string verificationToken) =>
        Task.CompletedTask;

    public Task SendPasswordResetEmail(string receiverName, string receiverEmailAddress, string resetToken) =>
        Task.CompletedTask;
}
