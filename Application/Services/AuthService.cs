using Application.ApiResult;
using Application.DTOs;
using Application.Identity;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class AuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(UserManager<User> userManager, IJwtTokenGenerator jwtTokenGenerator, IDateTimeProvider dateTimeProvider)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(AuthRegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || request.BirthDate == default)
        {
            return Result<AuthResponseDto>.Fail("All fields are required.", ErrorType.Validation);
        }

        var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existing != null)
        {
            return Result<AuthResponseDto>.Fail("Email is already taken.", ErrorType.Conflict);
        }

        var user = new User
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            BirthDate = request.BirthDate
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var message = string.Join("; ", identityResult.Errors.Select(x => x.Description));
            return Result<AuthResponseDto>.Fail(message, ErrorType.Validation);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, UserRole.Reader.ToString());
        if (!roleResult.Succeeded)
        {
            return Result<AuthResponseDto>.Fail("Unable to assign reader role.", ErrorType.Validation);
        }

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Role = UserRole.Reader.ToString()
        };

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email ?? string.Empty, $"{user.FirstName} {user.LastName}".Trim(), UserRole.Reader.ToString());

        return Result<AuthResponseDto>.Success(new AuthResponseDto { Token = token, User = dto });
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(AuthLoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponseDto>.Fail("Email and password are required.", ErrorType.Validation);
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
        {
            return Result<AuthResponseDto>.Fail("Invalid credentials.", ErrorType.Unauthorized);
        }

        var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValid)
        {
            return Result<AuthResponseDto>.Fail("Invalid credentials.", ErrorType.Unauthorized);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? UserRole.Reader.ToString();
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email ?? string.Empty, $"{user.FirstName} {user.LastName}".Trim(), role);

        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Role = role
        };

        return Result<AuthResponseDto>.Success(new AuthResponseDto { Token = token, User = dto });
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<UserDto>.Fail("User not found.", ErrorType.NotFound);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            Role = roles.FirstOrDefault() ?? UserRole.Reader.ToString()
        };

        return Result<UserDto>.Success(dto);
    }
}
