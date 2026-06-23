using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Application.DTOs
{
    public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string Role = "User"
);

    public record LoginRequest(
        string Username,
        string Password
    );

    public record AuthResponse(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt,
        string Username,
        string Role
    );

    public record RefreshTokenRequest(
        string AccessToken,
        string RefreshToken
    );
}
