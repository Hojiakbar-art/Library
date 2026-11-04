using Application.DTOs;
using Application.Services;
using Domain.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[Route("api/books")]
public class BooksController : ApiController
{
    private readonly BookService _bookService;

    public BooksController(BookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] BookFilter filter)
    {
        var result = await _bookService.GetBooksAsync(filter);
        return ToResponse(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _bookService.GetByIdAsync(id);
        return ToResponse(result);
    }

    [Authorize(Roles = "Librarian,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookRequestDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized(new { error = "User not identified." });
        }

        var result = await _bookService.CreateAsync(request, parsedUserId);
        return ToResponse(result);
    }

    [Authorize(Roles = "Librarian,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookRequestDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");
        if (!int.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized(new { error = "User not identified." });
        }

        var result = await _bookService.UpdateAsync(id, request, parsedUserId, isAdmin);
        return ToResponse(result);
    }
}
