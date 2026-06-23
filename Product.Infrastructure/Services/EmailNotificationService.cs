using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Product.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Infrastructure.Services
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly string _fromAddress;
        private readonly bool _isEnabled;

        public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _fromAddress = _configuration["Email:FromAddress"] ?? "noreply@productapi.com";
            _isEnabled = bool.TryParse(_configuration["Email:Enabled"], out var enabled) && enabled;
        }

        public async Task SendProductCreatedNotificationAsync(
            string recipientEmail, string productName, string createdBy, CancellationToken ct = default)
        {
            var subject = $"New Product Created: {productName}";
            var body = $"""
            A new product has been created.

            Product Name : {productName}
            Created By   : {createdBy}
            Created On   : {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC

            This is an automated notification from Product Management API.
            """;

            await SendAsync(recipientEmail, subject, body, ct);
        }

        public async Task SendProductDeletedNotificationAsync(
            string recipientEmail, string productName, CancellationToken ct = default)
        {
            var subject = $"Product Deleted: {productName}";
            var body = $"""
            A product has been permanently deleted.

            Product Name : {productName}
            Deleted On   : {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC

            This is an automated notification from Product Management API.
            """;

            await SendAsync(recipientEmail, subject, body, ct);
        }

        private async Task SendAsync(string to, string subject, string body, CancellationToken ct)
        {
            if (!_isEnabled)
            {
                // Log instead of sending when email is disabled (dev/test environments)
                _logger.LogInformation(
                    "[EmailNotificationService] Email suppressed (disabled). To={To} Subject={Subject}",
                    to, subject);
                return;
            }

            try
            {
                // ── Swap this block for your real provider SDK ──────────────────
                // Example: await _sendGridClient.SendEmailAsync(msg, ct);
                // ────────────────────────────────────────────────────────────────
                _logger.LogInformation(
                    "[EmailNotificationService] Sending email. From={From} To={To} Subject={Subject}",
                    _fromAddress, to, subject);

                await Task.Delay(10, ct); // simulate async I/O

                _logger.LogInformation("[EmailNotificationService] Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                // Non-fatal: log and swallow so email failure never breaks API operations
                _logger.LogWarning(ex, "[EmailNotificationService] Failed to send email to {To}", to);
            }
        }
    }
}
