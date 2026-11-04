using Application.DTOs;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[Route("api/users")]
public class UsersController : ApiController
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(new { items = Array.Empty<UserDto>(), page, pageSize, totalCount = 0, totalPages = 0 });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] RoleUpdateRequestDto request)
    {
        var result = await _userService.ChangeRoleAsync(id, request.Role);
        return ToResponse(result);
    }
}
