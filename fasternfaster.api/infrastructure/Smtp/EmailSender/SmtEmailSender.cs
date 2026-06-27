using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.Web.Options.App;
using FasterNFaster.Api.Web.Options.Smtp;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FasterNFaster.Api.Infrastructure.Smtp.EmailSender;

public class SmtpEmailSender(IOptions<SmtpOptions> smtp, IOptions<AppOptions> app) : IEmailSender
{
    private readonly SmtpOptions smtp = smtp.Value;
    private readonly AppOptions app = app.Value;
    public async Task SendConfirmationEmail(string receiverName, string receiverEmailAddress, string verificationToken)
    {
        var body = new TextPart("html")
        {
            Text = $$"""<a href="{{app.FrontendUrl}}/verify-email?token={{verificationToken}}">Click here to confirm</a>"""
        };
        await SendAsync(receiverName, receiverEmailAddress, "Confirm your email", body);
    }

    public async Task SendPasswordResetEmail(string receiverName, string receiverEmailAddress, string resetToken)
    {
        var body = new TextPart("html")
        {
            Text = $$"""<a href="{{app.FrontendUrl}}/reset-password?token={{resetToken}}">Click here to reset your password</a>"""
        };
        await SendAsync(receiverName, receiverEmailAddress, "Reset your password", body);
    }

    private async Task SendAsync(string receiverName, string receiverEmailAddress, string subject, MimeEntity body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        message.To.Add(new MailboxAddress(receiverName, receiverEmailAddress));
        message.Subject = subject;
        message.Body = body;

        var secureOptions = smtp.UseStartTls
            ? MailKit.Security.SecureSocketOptions.StartTls
            : MailKit.Security.SecureSocketOptions.None;

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, secureOptions);
        if (!string.IsNullOrEmpty(smtp.Username))
            await client.AuthenticateAsync(smtp.Username, smtp.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
