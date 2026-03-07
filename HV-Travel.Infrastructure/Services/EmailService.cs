using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using HVTravel.Application.Interfaces;

namespace HVTravel.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try 
            {
                var email = new MimeMessage();
                
                // Fallback to _config in case variables were actually loaded there, but prioritize Environment Variables
                var fromName = Environment.GetEnvironmentVariable("MAIL_FROM_NAME") ?? _config["MAIL_FROM_NAME"] ?? "HV Travel";
                var fromAddress = Environment.GetEnvironmentVariable("SMTP_USER") ?? _config["SMTP_USER"] ?? "";
                
                email.From.Add(new MailboxAddress(fromName, fromAddress));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = body };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                
                var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? _config["SMTP_HOST"];
                var portStr = Environment.GetEnvironmentVariable("SMTP_PORT") ?? _config["SMTP_PORT"] ?? "587";
                var port = int.Parse(portStr);
                
                var secureStr = Environment.GetEnvironmentVariable("SMTP_SECURE") ?? _config["SMTP_SECURE"] ?? "true";
                var secure = bool.Parse(secureStr);
                
                var secureSocketOptions = secure ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

                await smtp.ConnectAsync(host, port, secureSocketOptions);
                
                var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? _config["SMTP_USER"];
                var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? _config["SMTP_PASS"];
                
                if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
                {
                    await smtp.AuthenticateAsync(smtpUser, smtpPass);
                }

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // In a production app, log this exception
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }
    }
}
