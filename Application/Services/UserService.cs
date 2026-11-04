using Application.ApiResult;
using Application.DTOs;
using Application.Identity;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class UserService
{
    private readonly UserManager<User> _userManager;

    public UserService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserDto>> ChangeRoleAsync(int userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<UserDto>.Fail("User not found.", ErrorType.NotFound);
        }

        if (!Enum.TryParse<UserRole>(roleName, true, out var parsedRole))
        {
            return Result<UserDto>.Fail("Role is invalid.", ErrorType.Validation);
        }

        if (parsedRole != UserRole.Librarian && parsedRole != UserRole.Reader)
        {
            return Result<UserDto>.Fail("Only Librarian or Reader role can be assigned.", ErrorType.Validation);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(UserRole.Admin.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            return Result<UserDto>.Fail("Admin role cannot be changed.", ErrorType.Validation);
        }

        foreach (var existingRole in currentRoles)
        {
            await _userManager.RemoveFromRoleAsync(user, existingRole);
        }

        await _userManager.AddToRoleAsync(user, parsedRole.ToString());

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Role = parsedRole.ToString()
        };

        return Result<UserDto>.Success(dto);
    }
}
