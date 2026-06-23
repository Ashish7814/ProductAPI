using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using System.Security.Claims;

namespace ProductAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Product.Application.DTOs.RegisterRequest request, CancellationToken ct)
        {
            var response = await _authService.RegisterAsync(request, ct);
            return CreatedAtAction(nameof(Login), response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Product.Application.DTOs.LoginRequest request, CancellationToken ct)
        {
            var response = await _authService.LoginAsync(request, ct);
            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
            var response = await _authService.RefreshTokenAsync(request, ct);
            return Ok(response);
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke(CancellationToken ct)
        {
            var username = User.FindFirstValue(ClaimTypes.Name)!;
            await _authService.RevokeAsync(username, ct);
            return NoContent();
        }
    }
}
