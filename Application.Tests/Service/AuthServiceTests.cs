using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Application.Services;
using Product.Domain.Entities;
using Product.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Service
{
    public class AuthServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IPasswordHasher> _hasherMock = new();
        private readonly Mock<ILogger<AuthService>> _loggerMock = new();
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<ApplicationUser>())).Returns("access-token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
            _hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");

            _sut = new AuthService(_uowMock.Object, _tokenServiceMock.Object, _hasherMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_WithNewUser_ReturnsAuthResponse()
        {
            _userRepoMock.Setup(r => r.ExistsAsync("newuser", "new@test.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApplicationUser u, CancellationToken _) => u);
            _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _sut.RegisterAsync(new RegisterRequest("newuser", "new@test.com", "Password1"));

            result.Should().NotBeNull();
            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("refresh-token");
            result.Username.Should().Be("newuser");
        }

        [Fact]
        public async Task RegisterAsync_WithExistingUser_ThrowsConflictException()
        {
            _userRepoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await _sut.Invoking(s => s.RegisterAsync(new RegisterRequest("existing", "e@e.com", "Pass1")))
                .Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task RegisterAsync_HashesPasswordBeforeStoring()
        {
            _userRepoMock.Setup(r => r.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _userRepoMock.Setup(r => r.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApplicationUser u, CancellationToken _) => u);
            _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _sut.RegisterAsync(new RegisterRequest("u", "u@u.com", "PlainPass1"));

            _hasherMock.Verify(h => h.Hash("PlainPass1"), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
        {
            var user = new ApplicationUser
            {
                Id = 1,
                Username = "user1",
                Email = "u@u.com",
                PasswordHash = "hashed-password",
                Role = "User"
            };
            _userRepoMock.Setup(r => r.GetByUsernameAsync("user1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _hasherMock.Setup(h => h.Verify("Password1", "hashed-password")).Returns(true);
            _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _sut.LoginAsync(new LoginRequest("user1", "Password1"));

            result.AccessToken.Should().Be("access-token");
            result.Username.Should().Be("user1");
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedException()
        {
            var user = new ApplicationUser
            {
                Username = "user1",
                PasswordHash = "hashed-password"
            };
            _userRepoMock.Setup(r => r.GetByUsernameAsync("user1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _hasherMock.Setup(h => h.Verify("WrongPass", "hashed-password")).Returns(false);

            await _sut.Invoking(s => s.LoginAsync(new LoginRequest("user1", "WrongPass")))
                .Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ThrowsUnauthorizedException()
        {
            _userRepoMock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApplicationUser?)null);

            await _sut.Invoking(s => s.LoginAsync(new LoginRequest("ghost", "Pass1")))
                .Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task RevokeAsync_ClearsRefreshToken()
        {
            var user = new ApplicationUser
            {
                Username = "user1",
                PasswordHash = "h",
                RefreshToken = "old-token",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
            };
            _userRepoMock.Setup(r => r.GetByUsernameAsync("user1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _sut.RevokeAsync("user1");

            user.RefreshToken.Should().BeNull();
            user.RefreshTokenExpiry.Should().BeNull();
        }
    }

}
