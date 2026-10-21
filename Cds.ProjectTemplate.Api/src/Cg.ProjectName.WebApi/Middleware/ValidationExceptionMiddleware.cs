using Cg.ProjectName.Domain.Exceptions;
using Cg.ProjectName.Infrastructure.CrossCutting.Shared.Validation;
using Cg.ProjectName.WebApi.Shared;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cg.ProjectName.WebApi.Middleware;

/// <summary>
/// Middleware global de tratamento de exceções. Traduz exceções conhecidas em
/// respostas HTTP consistentes (<see cref="ApiResponse"/>) e garante que
/// qualquer exceção não mapeada explicitamente também vira uma resposta
/// controlada — nunca um 500 cru com stack trace vazando para o cliente.
/// </summary>
public class ValidationExceptionMiddleware(
    RequestDelegate next,
    ILogger<ValidationExceptionMiddleware> logger,
    IHostEnvironment environment)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ValidationExceptionMiddleware> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    // SQL Server: 2601 = duplicate key em índice único, 2627 = violação de
    // PRIMARY KEY/UNIQUE CONSTRAINT. Usado para reconhecer conflitos de
    // concorrência (ex.: duas requisições criando o mesmo registro único)
    // e devolver 409 Conflict em vez de um 500 genérico.
    private const int SqlErrorUniqueIndexViolation = 2601;
    private const int SqlErrorUniqueConstraintViolation = 2627;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                ex.Errors.Select(error => (ValidationErrorDetail)error));
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteResponseAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Unauthorized Access",
                [new ValidationErrorDetail { Error = ex.Message }]);
        }
        catch (NotFoundException ex)
        {
            // Único tipo de exceção usado pela aplicação para sinalizar
            // "recurso não encontrado" — os handlers antes lançavam
            // KeyNotFoundException (uma exceção do BCL pensada para lookup
            // em dicionário, não para 404 de domínio) em paralelo a esta,
            // exigindo dois catches idênticos aqui. Consolidado em um só.
            await WriteResponseAsync(
                context,
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                [new ValidationErrorDetail { Error = ex.Message }]);
        }
        catch (ArgumentException ex)
        {
            await WriteResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Invalid Request",
                [new ValidationErrorDetail { Error = ex.Message }]);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // DbUpdateConcurrencyException deriva de DbUpdateException — este
            // catch precisa vir antes dos catches de DbUpdateException logo
            // abaixo, senão nunca seria alcançado. Sinaliza que o registro foi
            // alterado (ou removido) por outra requisição entre a leitura e a
            // gravação — ver RowVersion em EntityAudititedAndSoftDeletable.
            _logger.LogWarning(ex, "Conflito de concorrência otimista (RowVersion) em {Path}", context.Request.Path);

            await WriteResponseAsync(
                context,
                StatusCodes.Status409Conflict,
                "Conflict",
                [new ValidationErrorDetail { Error = "O recurso foi alterado por outra requisição enquanto esta era processada. Recarregue os dados e tente novamente." }]);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Já é esperado que casos comuns (ex.: nomes únicos) sejam resolvidos
            // dentro do próprio handler/repositório (ver DemoOfficeWriterRepository).
            // Este catch é uma rede de segurança para violações de índice único
            // não tratadas explicitamente em algum outro fluxo futuro.
            _logger.LogWarning(ex, "Conflito de concorrência (violação de índice/constraint único) em {Path}", context.Request.Path);

            await WriteResponseAsync(
                context,
                StatusCodes.Status409Conflict,
                "Conflict",
                [new ValidationErrorDetail { Error = "O recurso já existe ou foi alterado por outra requisição. Tente novamente." }]);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Falha ao persistir alterações no banco de dados em {Path}", context.Request.Path);

            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Database Error",
                [new ValidationErrorDetail { Error = "Não foi possível salvar as alterações. Tente novamente mais tarde." }]);
        }
        catch (Exception ex)
        {
            // Rede de segurança final: qualquer exceção não mapeada é logada
            // por completo no servidor, mas o cliente nunca recebe stack trace
            // ou detalhes internos — apenas em ambiente de desenvolvimento o
            // corpo da resposta inclui a mensagem da exceção, para facilitar o debug local.
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Exceção não tratada em {Path}, mas a resposta já havia começado a ser enviada.", context.Request.Path);
                throw;
            }

            _logger.LogError(ex, "Exceção não tratada em {Path}. TraceId: {TraceId}", context.Request.Path, context.TraceIdentifier);

            var message = _environment.IsDevelopment()
                ? ex.Message
                : "Ocorreu um erro inesperado. Contate o suporte informando o identificador de rastreamento.";

            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                [new ValidationErrorDetail { Error = message, Detail = context.TraceIdentifier }]);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sqlException
           && (sqlException.Number == SqlErrorUniqueIndexViolation
               || sqlException.Number == SqlErrorUniqueConstraintViolation);

    private static Task WriteResponseAsync(
        HttpContext context,
        int statusCode,
        string message,
        IEnumerable<ValidationErrorDetail> errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
