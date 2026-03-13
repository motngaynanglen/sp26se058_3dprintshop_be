using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using sp26se058_3dprintshop_be.Application.Common.Config;
using sp26se058_3dprintshop_be.Application.Common.Interfaces;

namespace sp26se058_3dprintshop_be.Infrastructure.Service;
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.FromEmail));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = body };
        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_emailSettings.FromEmail, _emailSettings.AppPassword);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendPasswordResetEmailAsync(string to, string userName, string resetToken)
    {
        string body = $@"
            <h1>Khôi phục mật khẩu</h1>
            <p>Chào {userName},</p>
            <p>Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản tại 3D Print Shop.</p>
            <p>Vui lòng sử dụng mã Token dưới đây để đặt lại mật khẩu (hết hạn sau 15 phút):</p>
            <h2 style='color:blue;'>{resetToken}</h2>
            <p>Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email này.</p>";

        await SendEmailAsync(to, "3D Print Shop - Password Reset", body);
    }
}
