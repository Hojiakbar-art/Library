using Application.DTOs;
using Application.Identity;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/auth")]
public class AuthController : ApiController
{
    private readonly AuthService _authService;
    private readonly UserManager<User> _userManager;

    public AuthController(AuthService authService, UserManager<User> userManager)
    {
        _authService = authService;
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return ToResponse(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthLoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return ToResponse(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { error = "User not identified." });
        }

        var result = await _authService.GetCurrentUserAsync(userId);
        return ToResponse(result);
    }
}
