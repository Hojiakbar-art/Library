using Application.DTOs;
using Application.Services;
using Domain.Enums;
using Domain.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[Route("api/loans")]
public class LoansController : ApiController
{
    private readonly LoanService _loanService;

    public LoansController(LoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpPost]
    public async Task<IActionResult> Borrow([FromBody] LoanRequestDto request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "User not identified." });
        }

        var result = await _loanService.BorrowAsync(userId, request.BookId);
        return ToResponse(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy([FromQuery] LoanStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "User not identified." });
        }

        var result = await _loanService.GetMyLoansAsync(userId, status, page, pageSize);
        return ToResponse(result);
    }

    [HttpPost("{id:int}/return")]
    public async Task<IActionResult> Return(int id)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "User not identified." });
        }

        var result = await _loanService.ReturnAsync(id, userId, User.IsInRole("Librarian"), User.IsInRole("Admin"));
        return ToResponse(result);
    }

    [Authorize(Roles = "Librarian,Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LoanFilter filter)
    {
        var result = await _loanService.GetAllAsync(filter);
        return ToResponse(result);
    }
}
