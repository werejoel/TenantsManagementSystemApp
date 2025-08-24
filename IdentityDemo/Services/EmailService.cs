﻿using TenantsManagementApp.Services;
using System.Net;
using System.Net.Mail;

namespace TenantsManagementApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailConfirmationCodeAsync(string toEmail, string firstName, string code)
        {
            string htmlContent = $@"
                    <html><body style='font-family: Arial, sans-serif; background-color: #f4f6f8; margin:0; padding:20px;'>
                      <div style='max-width:600px; margin:auto; background:#fff; padding:30px; border-radius:8px;'>
                        <h2 style='color:#333;'>Welcome, {firstName}!</h2>
                        <p style='font-size:16px; color:#555;'>Thank you for registering. Please confirm your email by entering the code below on the confirmation page.</p>
                        <div style='font-size:2rem; font-weight:bold; color:#0d6efd; margin:20px 0;'>{code}</div>
                        <p style='font-size:12px; color:#999; margin-top:30px;'>&copy; {DateTime.UtcNow.Year} Dot Net Tutorials. All rights reserved.</p>
                      </div>
                    </body></html>";

            await SendEmailAsync(toEmail, "Your Email Confirmation Code", htmlContent, true);
        }

        public async Task SendRegistrationConfirmationEmailAsync(string toEmail, string firstName, string confirmationLink)
        {
            string htmlContent = $@"
                    <html><body style='font-family: Arial, sans-serif; background-color: #f4f6f8; margin:0; padding:20px;'>
                      <div style='max-width:600px; margin:auto; background:#fff; padding:30px; border-radius:8px;'>
                        <h2 style='color:#333;'>Welcome, {firstName}!</h2>
                        <p style='font-size:16px; color:#555;'>Thank you for registering. Please confirm your email by clicking the button below.</p>
                        <p style='text-align:center;'>
                          <a href='{confirmationLink}' style='background:#0d6efd; color:#fff; padding:12px 24px; border-radius:6px; text-decoration:none; font-weight:bold;'>Confirm Your Email</a>
                        </p>
                        <p style='font-size:12px; color:#999; margin-top:30px;'>&copy; {DateTime.UtcNow.Year} Dot Net Tutorials. All rights reserved.</p>
                      </div>
                    </body></html>";

            await SendEmailAsync(toEmail, "Email Confirmation", htmlContent, true);
        }

        public async Task SendAccountCreatedEmailAsync(string toEmail, string firstName, string loginLink)
        {
            string htmlContent = $@"
                    <html><body style='font-family: Arial, sans-serif; background-color: #f4f6f8; margin:0; padding:20px;'>
                      <div style='max-width:600px; margin:auto; background:#fff; padding:30px; border-radius:8px;'>
                        <h2 style='color:#333;'>Hello, {firstName}!</h2>
                        <p style='font-size:16px; color:#555;'>Your account has been successfully created and your email is confirmed.</p>
                        <p style='text-align:center;'>
                          <a href='{loginLink}' style='background:#198754; color:#fff; padding:12px 24px; border-radius:6px; text-decoration:none; font-weight:bold;'>Login to Your Account</a>
                        </p>
                        <p style='font-size:12px; color:#999; margin-top:30px;'>&copy; {DateTime.UtcNow.Year} Dot Net Tutorials. All rights reserved.</p>
                      </div>
                    </body></html>";

            await SendEmailAsync(toEmail, "Account Created - Dot Net Tutorials", htmlContent, true);
        }

        public async Task SendResendConfirmationEmailAsync(string toEmail, string firstName, string confirmationLink)
        {
            string htmlContent = $@"
                    <html><body style='font-family: Arial, sans-serif; background-color: #f4f6f8; margin:0; padding:20px;'>
                      <div style='max-width:600px; margin:auto; background:#fff; padding:30px; border-radius:8px;'>
                        <h2 style='color:#333;'>Hello, {firstName}!</h2>
                        <p style='font-size:16px; color:#555;'>You requested a new email confirmation link. Please confirm your email by clicking the button below.</p>
                        <p style='text-align:center;'>
                          <a href='{confirmationLink}' style='background:#0d6efd; color:#fff; padding:12px 24px; border-radius:6px; text-decoration:none; font-weight:bold;'>Confirm Your Email</a>
                        </p>
                        <p style='font-size:12px; color:#999; margin-top:30px;'>&copy; {DateTime.UtcNow.Year} Dot Net Tutorials. All rights reserved.</p>
                      </div>
                    </body></html>";

            await SendEmailAsync(toEmail, "Email Confirmation - Dot Net Tutorials", htmlContent, true);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body, bool isBodyHtml = false)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var password = _configuration["EmailSettings:Password"];

                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail!, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isBodyHtml
                };
                message.To.Add(new MailAddress(toEmail));

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, password),
                    EnableSsl = true
                };

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} with subject {Subject}", toEmail, subject);
            }
        }
    }
}
