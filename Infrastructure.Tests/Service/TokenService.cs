using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Product.Domain.Entities;
using Product.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Service
{
    public class TokenServiceTests
    {
        private static TokenService CreateSut(int expiryMinutes = 15)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = "TestSuperSecretKeyThatIsAtLeast32Chars!",
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience",
                    ["JwtSettings:ExpiryMinutes"] = expiryMinutes.ToString()
                })
                .Build();

            return new TokenService(config);
        }

        private static ApplicationUser BuildUser(string role = "User") => new()
        {
            Id = 42,
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = role
        };

        // ── GenerateAccessToken ───────────────────────────────────────────────

        [Fact]
        public void GenerateAccessToken_ReturnsNonEmptyString()
        {
            var sut = CreateSut();
            var token = sut.GenerateAccessToken(BuildUser());
            token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GenerateAccessToken_IsValidJwtFormat()
        {
            var sut = CreateSut();
            var token = sut.GenerateAccessToken(BuildUser());

            // A valid JWT has exactly 3 dot-separated Base64 segments
            token.Split('.').Should().HaveCount(3);
        }

        [Fact]
        public void GenerateAccessToken_ContainsExpectedClaims()
        {
            var sut = CreateSut();
            var user = BuildUser("Admin");
            var token = sut.GenerateAccessToken(user);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                .Should().Be("testuser");
            jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                .Should().Be("Admin");
            jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                .Should().Be("test@test.com");
        }

        [Fact]
        public void GenerateAccessToken_HasCorrectIssuerAndAudience()
        {
            var sut = CreateSut();
            var token = sut.GenerateAccessToken(BuildUser());
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            jwt.Issuer.Should().Be("TestIssuer");
            jwt.Audiences.Should().Contain("TestAudience");
        }

        [Fact]
        public void GenerateAccessToken_ExpiresAfterConfiguredMinutes()
        {
            var sut = CreateSut(expiryMinutes: 30);
            var before = DateTime.UtcNow.AddMinutes(29);
            var after = DateTime.UtcNow.AddMinutes(31);

            var token = sut.GenerateAccessToken(BuildUser());
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            jwt.ValidTo.Should().BeAfter(before).And.BeBefore(after);
        }

        // ── GenerateRefreshToken ──────────────────────────────────────────────

        [Fact]
        public void GenerateRefreshToken_ReturnsNonEmptyString()
        {
            var sut = CreateSut();
            var rt = sut.GenerateRefreshToken();
            rt.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GenerateRefreshToken_IsBase64Encoded()
        {
            var sut = CreateSut();
            var rt = sut.GenerateRefreshToken();
            var act = () => Convert.FromBase64String(rt);
            act.Should().NotThrow();
        }

        [Fact]
        public void GenerateRefreshToken_IsDifferentEachCall()
        {
            var sut = CreateSut();
            var t1 = sut.GenerateRefreshToken();
            var t2 = sut.GenerateRefreshToken();
            t1.Should().NotBe(t2);
        }

        [Fact]
        public void GenerateRefreshToken_Has64ByteEntropy()
        {
            var sut = CreateSut();
            var rt = sut.GenerateRefreshToken();
            var bytes = Convert.FromBase64String(rt);
            bytes.Should().HaveCount(64);
        }

        // ── GetPrincipalFromExpiredToken ──────────────────────────────────────

        [Fact]
        public void GetPrincipalFromExpiredToken_ValidExpiredToken_ReturnsPrincipal()
        {
            // Generate with 0-minute expiry so it's already expired
            var sut = CreateSut(expiryMinutes: 0);
            var token = sut.GenerateAccessToken(BuildUser());

            var principal = sut.GetPrincipalFromExpiredToken(token);

            principal.Should().NotBeNull();
            principal!.Identity?.Name.Should().Be("testuser");
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ValidActiveToken_ReturnsPrincipal()
        {
            var sut = CreateSut(expiryMinutes: 60);
            var token = sut.GenerateAccessToken(BuildUser());

            var principal = sut.GetPrincipalFromExpiredToken(token);

            principal.Should().NotBeNull();
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_GarbageToken_ReturnsNull()
        {
            var sut = CreateSut();
            var principal = sut.GetPrincipalFromExpiredToken("not.a.valid.jwt.token");
            principal.Should().BeNull();
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_EmptyString_ReturnsNull()
        {
            var sut = CreateSut();
            var principal = sut.GetPrincipalFromExpiredToken(string.Empty);
            principal.Should().BeNull();
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_TokenFromDifferentKey_ReturnsNull()
        {
            // Token signed with a different key should fail signature validation
            var sutA = CreateSut();
            var configB = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = "DifferentSecretKeyThatIsAlso32Chars!",
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience",
                    ["JwtSettings:ExpiryMinutes"] = "15"
                })
                .Build();
            var sutB = new TokenService(configB);

            var tokenFromA = sutA.GenerateAccessToken(BuildUser());
            var principal = sutB.GetPrincipalFromExpiredToken(tokenFromA);

            principal.Should().BeNull();
        }
    }

}
