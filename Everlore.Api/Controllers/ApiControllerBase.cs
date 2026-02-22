using Everlore.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToError(Result result)
    {
        if (result.IsSuccess)
            return Ok();

        return result.Error!.Type switch
        {
            ResultErrorType.NotFound => NotFound(ToProblem(result.Error, StatusCodes.Status404NotFound)),
            ResultErrorType.Validation => UnprocessableEntity(ToProblem(result.Error, StatusCodes.Status422UnprocessableEntity)),
            ResultErrorType.Conflict => Conflict(ToProblem(result.Error, StatusCodes.Status409Conflict)),
            ResultErrorType.Unauthorized => Unauthorized(ToProblem(result.Error, StatusCodes.Status401Unauthorized)),
            ResultErrorType.Forbidden => new ObjectResult(ToProblem(result.Error, StatusCodes.Status403Forbidden)) { StatusCode = StatusCodes.Status403Forbidden },
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private static ProblemDetails ToProblem(ResultError error, int statusCode) => new()
    {
        Status = statusCode,
        Title = error.Type.ToString(),
        Detail = error.Message
    };
}
