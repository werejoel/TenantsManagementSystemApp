﻿namespace TenantsManagementApp.Services
{
    public interface IEmailService
    {
      
        Task SendEmailConfirmationCodeAsync(string toEmail, string firstName, string code);
        Task SendRegistrationConfirmationEmailAsync(string toEmail, string firstName, string confirmationLink);
        Task SendAccountCreatedEmailAsync(string toEmail, string firstName, string loginLink);
        Task SendResendConfirmationEmailAsync(string toEmail, string firstName, string confirmationLink);
    }
}