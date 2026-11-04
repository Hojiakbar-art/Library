using Application.ApiResult;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiController : ControllerBase
{
    protected ActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(new { error = result.ErrorMessage }),
            ErrorType.Unauthorized => Unauthorized(new { error = result.ErrorMessage }),
            ErrorType.Forbidden => StatusCode(403, new { error = result.ErrorMessage }),
            ErrorType.NotFound => NotFound(new { error = result.ErrorMessage }),
            ErrorType.Conflict => Conflict(new { error = result.ErrorMessage }),
            _ => StatusCode(500, new { error = result.ErrorMessage ?? "Internal server error" })
        };
    }
}
