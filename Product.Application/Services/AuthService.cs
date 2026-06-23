using Microsoft.Extensions.Logging;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using Product.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork uow,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            ILogger<AuthService> logger)
        {
            _uow = uow;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            if (await _uow.Users.ExistsAsync(request.Username, request.Email, ct))
                throw new ConflictException("Username or email already in use.");

            var user = new ApplicationUser
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = request.Role,
                CreatedOn = DateTime.UtcNow
            };

            await _uow.Users.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("User {Username} registered", request.Username);
            return await GenerateTokensAsync(user, ct);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByUsernameAsync(request.Username, ct)
                ?? throw new UnauthorizedException("Invalid credentials.");

            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid credentials.");

            _logger.LogInformation("User {Username} logged in", request.Username);
            return await GenerateTokensAsync(user, ct);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken)
                ?? throw new UnauthorizedException("Invalid access token.");

            var username = principal.Identity?.Name
                ?? throw new UnauthorizedException("Invalid token claims.");

            var user = await _uow.Users.GetByUsernameAsync(username, ct)
                ?? throw new UnauthorizedException("User not found.");

            if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
                throw new UnauthorizedException("Invalid or expired refresh token.");

            return await GenerateTokensAsync(user, ct);
        }

        public async Task RevokeAsync(string username, CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByUsernameAsync(username, ct)
                ?? throw new NotFoundException(nameof(ApplicationUser), username);

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _uow.Users.UpdateAsync(user, ct);
            await _uow.SaveChangesAsync(ct);
        }

        private async Task<AuthResponse> GenerateTokensAsync(ApplicationUser user, CancellationToken ct)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _uow.Users.UpdateAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

            return new AuthResponse(
                accessToken,
                refreshToken,
                DateTime.UtcNow.AddMinutes(15),
                user.Username,
                user.Role
            );
        }
    }
}
