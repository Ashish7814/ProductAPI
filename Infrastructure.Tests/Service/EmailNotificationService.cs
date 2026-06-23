using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Service
{
    public class EmailNotificationServiceTests
    {
        private static EmailNotificationService CreateSut(bool enabled = false)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Enabled"] = enabled.ToString().ToLower(),
                    ["Email:FromAddress"] = "noreply@test.com"
                })
                .Build();

            return new EmailNotificationService(config, NullLogger<EmailNotificationService>.Instance);
        }

        [Fact]
        public async Task SendProductCreatedNotificationAsync_WhenDisabled_DoesNotThrow()
        {
            var sut = CreateSut(enabled: false);

            var act = async () => await sut.SendProductCreatedNotificationAsync(
                "admin@test.com", "Widget A", "john");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendProductDeletedNotificationAsync_WhenDisabled_DoesNotThrow()
        {
            var sut = CreateSut(enabled: false);

            var act = async () => await sut.SendProductDeletedNotificationAsync(
                "admin@test.com", "Widget A");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendProductCreatedNotificationAsync_WhenEnabled_DoesNotThrow()
        {
            // When enabled=true but no real SMTP, the service logs and swallows the error
            var sut = CreateSut(enabled: true);

            var act = async () => await sut.SendProductCreatedNotificationAsync(
                "admin@test.com", "Gadget Pro", "alice");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendProductDeletedNotificationAsync_WithEmptyRecipient_DoesNotThrow()
        {
            var sut = CreateSut(enabled: false);

            var act = async () => await sut.SendProductDeletedNotificationAsync(
                string.Empty, "Product X");

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendProductCreatedNotificationAsync_CompletesWithCancellation()
        {
            var sut = CreateSut(enabled: false);
            using var cts = new CancellationTokenSource();

            var act = async () => await sut.SendProductCreatedNotificationAsync(
                "user@test.com", "Test Product", "bob", cts.Token);

            await act.Should().NotThrowAsync();
        }
    }

}
