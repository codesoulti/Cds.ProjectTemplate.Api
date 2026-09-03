using Cg.ProjectName.Application.Shared.Paginations.Dapper;
using Cg.ProjectName.WebApi.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cg.ProjectName.WebApi.Controllers.Base;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    // Nenhum esquema de autenticação está configurado ainda nesta aplicação
    // (ver comentário em HangfireConfiguration/LocalRequestsOnlyAuthorizationFilter) —
    // ou seja, User.Identity nunca vem populado hoje. Antes, o primeiro
    // chamador destes métodos estourava um NullReferenceException cru (500
    // opaco). Lançar UnauthorizedAccessException em vez disso aproveita o
    // catch já existente em ValidationExceptionMiddleware, que traduz para
    // um 401 claro — e continua funcionando normalmente no dia em que a
    // autenticação real for adicionada.
    protected int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    protected string GetCurrentUserEmail() =>
        User.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

    protected IActionResult Ok<T>(T data, string? message) =>
            base.Ok(new ApiResponseWithData<T> { Data = data, Message = message ?? "", Success = true });

    protected IActionResult Created<T>(string routeName, object routeValues, T data) =>
        base.CreatedAtRoute(routeName, routeValues, new ApiResponseWithData<T> { Data = data, Success = true });

    protected IActionResult BadRequest(string message) =>
        base.BadRequest(new ApiResponse { Message = message, Success = false });

    protected IActionResult NotFound(string message = "Resource not found") =>
        base.NotFound(new ApiResponse { Message = message, Success = false });

    protected IActionResult OkPaginated<T>(DapperPaginatedListDto<T> pagedList) =>
            Ok(new PaginatedResponse<T>
            {
                Data = pagedList.Items,
                CurrentPage = pagedList.CurrentPage,
                TotalPages = pagedList.TotalPages,
                TotalCount = pagedList.TotalCount,
                Success = true
            });
}
